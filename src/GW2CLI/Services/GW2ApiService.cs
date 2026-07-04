using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GW2CLI.Models;

namespace GW2CLI.Services;

public class GW2ApiService(ConfigService config)
{
    private const string BaseUrl = "https://api.guildwars2.com/v2";
    private const int BatchSize = 200;

    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private string? _overrideKey;

    public void SetOverrideKey(string? key) => _overrideKey = key;

    public string? EffectiveKey => _overrideKey ?? config.ApiKey;

    private HttpRequestMessage BuildRequest(string path)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{path}");
        var key = EffectiveKey;
        if (key != null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return req;
    }

    private async Task<T> GetAsync<T>(string path)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await Http.SendAsync(BuildRequest(path));
        }
        catch (HttpRequestException ex)
        {
            throw new GW2ApiException($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            throw new GW2ApiException("Request timed out. Check your internet connection.");
        }

        switch (resp.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                throw new GW2ApiException("Invalid or missing API key. Run 'gw2 auth set <key>' to configure.");
            case HttpStatusCode.Forbidden:
                throw new GW2ApiException("API key lacks required permissions for this endpoint.");
            case HttpStatusCode.NotFound:
                throw new GW2ApiException($"Not found: {path}");
            case HttpStatusCode.TooManyRequests:
                throw new GW2ApiException("Rate limit exceeded. Wait a moment and try again.");
            case HttpStatusCode.ServiceUnavailable:
                throw new GW2ApiException("GW2 API is temporarily unavailable.");
        }

        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)
               ?? throw new GW2ApiException("Empty response from API.");
    }

    private async Task<List<T>> GetBatchAsync<T>(string endpoint, IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];
        var results = new List<T>();
        foreach (var batch in idList.Chunk(BatchSize))
        {
            var items = await GetAsync<List<T>>($"{endpoint}?ids={string.Join(",", batch)}");
            results.AddRange(items);
        }
        return results;
    }

    // ── Account ────────────────────────────────────────────────────────────────
    public Task<Account> GetAccountAsync() => GetAsync<Account>("account");
    public Task<List<BankSlot?>> GetBankAsync() => GetAsync<List<BankSlot?>>("account/bank");
    public Task<List<WalletEntry>> GetWalletAsync() => GetAsync<List<WalletEntry>>("account/wallet");
    public Task<List<InventorySlot?>> GetSharedInventoryAsync() => GetAsync<List<InventorySlot?>>("account/inventory");
    public Task<List<AccountAchievement>> GetAccountAchievementsAsync() =>
        GetAsync<List<AccountAchievement>>("account/achievements");
    public Task<List<AccountAchievement>> GetAccountAchievementsByIdAsync(IEnumerable<int> ids) =>
        GetAsync<List<AccountAchievement>>($"account/achievements?ids={string.Join(",", ids)}");
    public Task<List<AccountMastery>> GetAccountMasteriesAsync() =>
        GetAsync<List<AccountMastery>>("account/masteries");
    public Task<List<string>> GetAccountMountsAsync() =>
        GetAsync<List<string>>("account/mounts/types");
    public Task<List<string>> GetAccountRaidClearancesAsync() =>
        GetAsync<List<string>>("account/raids");
    public Task<List<string>> GetAccountDungeonClearancesAsync() =>
        GetAsync<List<string>>("account/dungeons");

    // ── Characters ─────────────────────────────────────────────────────────────
    public Task<List<string>> GetCharacterNamesAsync() => GetAsync<List<string>>("characters");
    public Task<Character> GetCharacterAsync(string name) =>
        GetAsync<Character>($"characters/{Uri.EscapeDataString(name)}");

    // ── Achievements ───────────────────────────────────────────────────────────
    public Task<List<Achievement>> GetAchievementsAsync(IEnumerable<int> ids) =>
        GetBatchAsync<Achievement>("achievements", ids);
    public Task<DailyAchievements> GetDailyAchievementsAsync() =>
        GetAsync<DailyAchievements>("achievements/daily");
    public Task<List<AchievementCategory>> GetAchievementCategoriesAsync() =>
        GetAsync<List<AchievementCategory>>("achievements/categories?ids=all");
    public Task<List<AchievementGroup>> GetAchievementGroupsAsync() =>
        GetAsync<List<AchievementGroup>>("achievements/groups?ids=all");

    // ── Items ──────────────────────────────────────────────────────────────────
    public Task<Item> GetItemAsync(int id) => GetAsync<Item>($"items/{id}");
    public Task<List<Item>> GetItemsAsync(IEnumerable<int> ids) => GetBatchAsync<Item>("items", ids);

    // ── Currencies ─────────────────────────────────────────────────────────────
    public Task<List<Currency>> GetAllCurrenciesAsync() =>
        GetAsync<List<Currency>>("currencies?ids=all");
    public Task<List<Currency>> GetCurrenciesAsync(IEnumerable<int> ids) =>
        GetBatchAsync<Currency>("currencies", ids);

    // ── Recipes ────────────────────────────────────────────────────────────────
    public Task<Recipe> GetRecipeAsync(int id) => GetAsync<Recipe>($"recipes/{id}");
    public Task<List<Recipe>> GetRecipesAsync(IEnumerable<int> ids) => GetBatchAsync<Recipe>("recipes", ids);
    public Task<List<int>> GetRecipesByOutputAsync(int itemId) =>
        GetAsync<List<int>>($"recipes/search?output={itemId}");
    public Task<List<int>> GetRecipesByInputAsync(int itemId) =>
        GetAsync<List<int>>($"recipes/search?input={itemId}");

    // ── Commerce ───────────────────────────────────────────────────────────────
    public Task<CommercePrices> GetItemPricesAsync(int id) =>
        GetAsync<CommercePrices>($"commerce/prices/{id}");
    public Task<List<CommercePrices>> GetItemPricesBatchAsync(IEnumerable<int> ids) =>
        GetBatchAsync<CommercePrices>("commerce/prices", ids);
    public Task<CommerceListings> GetItemListingsAsync(int id) =>
        GetAsync<CommerceListings>($"commerce/listings/{id}");
    public Task<ExchangeRate> GetCoinToGemRateAsync(long coins) =>
        GetAsync<ExchangeRate>($"commerce/exchange/coins?quantity={coins}");
    public Task<ExchangeRate> GetGemToCoinRateAsync(int gems) =>
        GetAsync<ExchangeRate>($"commerce/exchange/gems?quantity={gems}");
    public Task<CommerceDelivery> GetDeliveryAsync() => GetAsync<CommerceDelivery>("commerce/delivery");

    // ── Worlds ─────────────────────────────────────────────────────────────────
    public Task<World> GetWorldAsync(int id) => GetAsync<World>($"worlds/{id}");

    // ── Skills / Specializations / Traits ──────────────────────────────────────
    public Task<List<Skill>> GetSkillsAsync(IEnumerable<int> ids) => GetBatchAsync<Skill>("skills", ids);
    public Task<List<Specialization>> GetSpecializationsAsync(IEnumerable<int> ids) =>
        GetBatchAsync<Specialization>("specializations", ids);
    public Task<List<Trait>> GetTraitsAsync(IEnumerable<int> ids) => GetBatchAsync<Trait>("traits", ids);
}
