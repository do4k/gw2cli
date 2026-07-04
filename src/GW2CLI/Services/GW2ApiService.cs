using GW2CLI.Models;

namespace GW2CLI.Services;

/// Thin wrapper over IGW2ApiClient that adds batching for endpoints that accept id lists.
public class GW2ApiService(IGW2ApiClient client)
{
    private const int BatchSize = 200;

    private async Task<List<T>> BatchAsync<T>(Func<IEnumerable<int>, Task<List<T>>> fetch, IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];
        var results = new List<T>();
        foreach (var batch in idList.Chunk(BatchSize))
            results.AddRange(await fetch(batch));
        return results;
    }

    // ── Account ────────────────────────────────────────────────────────────────
    public Task<Account> GetAccountAsync() => client.GetAccountAsync();
    public Task<List<BankSlot?>> GetBankAsync() => client.GetBankAsync();
    public Task<List<WalletEntry>> GetWalletAsync() => client.GetWalletAsync();
    public Task<List<InventorySlot?>> GetSharedInventoryAsync() => client.GetSharedInventoryAsync();
    public Task<List<AccountAchievement>> GetAccountAchievementsAsync() => client.GetAllAccountAchievementsAsync();
    public Task<List<AccountAchievement>> GetAccountAchievementsByIdAsync(IEnumerable<int> ids) =>
        client.GetAccountAchievementsByIdAsync(ids);
    public Task<List<AccountMastery>> GetAccountMasteriesAsync() => client.GetAccountMasteriesAsync();
    public Task<List<string>> GetAccountMountsAsync() => client.GetAccountMountsAsync();
    public Task<List<string>> GetAccountRaidClearancesAsync() => client.GetAccountRaidClearancesAsync();
    public Task<List<string>> GetAccountDungeonClearancesAsync() => client.GetAccountDungeonClearancesAsync();
    public Task<List<LegendaryArmoryEntry>> GetLegendaryArmoryAsync() => client.GetLegendaryArmoryAsync();
    public Task<WizardsVaultResponse> GetWizardsVaultDailyAsync() => client.GetWizardsVaultDailyAsync();
    public Task<WizardsVaultResponse> GetWizardsVaultWeeklyAsync() => client.GetWizardsVaultWeeklyAsync();

    // ── Characters ─────────────────────────────────────────────────────────────
    public Task<List<string>> GetCharacterNamesAsync() => client.GetCharacterNamesAsync();
    public Task<Character> GetCharacterAsync(string name) => client.GetCharacterAsync(name);

    // ── Achievements ───────────────────────────────────────────────────────────
    public Task<List<Achievement>> GetAchievementsAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetAchievementsAsync, ids);
    public Task<DailyAchievements> GetDailyAchievementsAsync() => client.GetDailyAchievementsAsync();
    public Task<List<AchievementCategory>> GetAchievementCategoriesAsync() => client.GetAchievementCategoriesAsync();
    public Task<List<AchievementGroup>> GetAchievementGroupsAsync() => client.GetAchievementGroupsAsync();

    // ── Masteries ──────────────────────────────────────────────────────────────
    public Task<List<MasteryTrack>> GetAllMasteryTracksAsync() => client.GetAllMasteryTracksAsync();

    // ── Items ──────────────────────────────────────────────────────────────────
    public Task<Item> GetItemAsync(int id) => client.GetItemAsync(id);
    public Task<List<Item>> GetItemsAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetItemsBatchAsync, ids);

    // ── Currencies ─────────────────────────────────────────────────────────────
    public Task<List<Currency>> GetAllCurrenciesAsync() => client.GetAllCurrenciesAsync();
    public Task<List<Currency>> GetCurrenciesAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetCurrenciesBatchAsync, ids);

    // ── Recipes ────────────────────────────────────────────────────────────────
    public Task<Recipe> GetRecipeAsync(int id) => client.GetRecipeAsync(id);
    public Task<List<Recipe>> GetRecipesAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetRecipesBatchAsync, ids);
    public Task<List<int>> GetRecipesByOutputAsync(int itemId) => client.GetRecipesByOutputAsync(itemId);
    public Task<List<int>> GetRecipesByInputAsync(int itemId) => client.GetRecipesByInputAsync(itemId);

    // ── Commerce ───────────────────────────────────────────────────────────────
    public Task<CommercePrices> GetItemPricesAsync(int id) => client.GetItemPricesAsync(id);
    public Task<List<CommercePrices>> GetItemPricesBatchAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetItemPricesBatchAsync, ids);
    public Task<CommerceListings> GetItemListingsAsync(int id) => client.GetItemListingsAsync(id);
    public Task<ExchangeRate> GetCoinToGemRateAsync(long coins) => client.GetCoinToGemRateAsync(coins);
    public Task<ExchangeRate> GetGemToCoinRateAsync(int gems) => client.GetGemToCoinRateAsync(gems);
    public Task<CommerceDelivery> GetDeliveryAsync() => client.GetDeliveryAsync();

    // ── Worlds ─────────────────────────────────────────────────────────────────
    public Task<World> GetWorldAsync(int id) => client.GetWorldAsync(id);

    // ── WvW ────────────────────────────────────────────────────────────────────
    public Task<List<WvwMatch>> GetWvwMatchesAsync() => client.GetWvwMatchesAsync();
    public Task<WvwMatch> GetWvwMatchByWorldAsync(int worldId) => client.GetWvwMatchByWorldAsync(worldId);

    // ── PvP ────────────────────────────────────────────────────────────────────
    public Task<PvpStats> GetPvpStatsAsync() => client.GetPvpStatsAsync();
    public Task<List<PvpStanding>> GetPvpStandingsAsync() => client.GetPvpStandingsAsync();

    // ── Skills / Specializations / Traits ──────────────────────────────────────
    public Task<List<Skill>> GetSkillsAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetSkillsAsync, ids);
    public Task<List<Specialization>> GetSpecializationsAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetSpecializationsAsync, ids);
    public Task<List<Trait>> GetTraitsAsync(IEnumerable<int> ids) =>
        BatchAsync(client.GetTraitsAsync, ids);
}
