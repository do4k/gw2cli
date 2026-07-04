using Refit;
using GW2CLI.Models;

namespace GW2CLI.Services;

[Headers("Accept: application/json")]
public interface IGW2ApiClient
{
    // ── Account ────────────────────────────────────────────────────────────────
    [Get("/account")]
    Task<Account> GetAccountAsync();

    [Get("/account/bank")]
    Task<List<BankSlot?>> GetBankAsync();

    [Get("/account/wallet")]
    Task<List<WalletEntry>> GetWalletAsync();

    [Get("/account/inventory")]
    Task<List<InventorySlot?>> GetSharedInventoryAsync();

    [Get("/account/achievements")]
    Task<List<AccountAchievement>> GetAllAccountAchievementsAsync();

    [Get("/account/achievements")]
    Task<List<AccountAchievement>> GetAccountAchievementsByIdAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    [Get("/account/masteries")]
    Task<List<AccountMastery>> GetAccountMasteriesAsync();

    [Get("/account/mounts/types")]
    Task<List<string>> GetAccountMountsAsync();

    [Get("/account/raids")]
    Task<List<string>> GetAccountRaidClearancesAsync();

    [Get("/account/dungeons")]
    Task<List<string>> GetAccountDungeonClearancesAsync();

    [Get("/account/legendaryarmory")]
    Task<List<LegendaryArmoryEntry>> GetLegendaryArmoryAsync();

    [Get("/account/wizardsvault/daily")]
    Task<WizardsVaultResponse> GetWizardsVaultDailyAsync();

    [Get("/account/wizardsvault/weekly")]
    Task<WizardsVaultResponse> GetWizardsVaultWeeklyAsync();

    // ── Characters ─────────────────────────────────────────────────────────────
    [Get("/characters")]
    Task<List<string>> GetCharacterNamesAsync();

    [Get("/characters/{name}")]
    Task<Character> GetCharacterAsync(string name);

    // ── Achievements ───────────────────────────────────────────────────────────
    [Get("/achievements")]
    Task<List<Achievement>> GetAchievementsAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    [Get("/achievements/daily")]
    Task<DailyAchievements> GetDailyAchievementsAsync();

    [Get("/achievements/categories?ids=all")]
    Task<List<AchievementCategory>> GetAchievementCategoriesAsync();

    [Get("/achievements/groups?ids=all")]
    Task<List<AchievementGroup>> GetAchievementGroupsAsync();

    // ── Masteries ──────────────────────────────────────────────────────────────
    [Get("/masteries?ids=all")]
    Task<List<MasteryTrack>> GetAllMasteryTracksAsync();

    // ── Items ──────────────────────────────────────────────────────────────────
    [Get("/items/{id}")]
    Task<Item> GetItemAsync(int id);

    [Get("/items")]
    Task<List<Item>> GetItemsBatchAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    // ── Currencies ─────────────────────────────────────────────────────────────
    [Get("/currencies?ids=all")]
    Task<List<Currency>> GetAllCurrenciesAsync();

    [Get("/currencies")]
    Task<List<Currency>> GetCurrenciesBatchAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    // ── Recipes ────────────────────────────────────────────────────────────────
    [Get("/recipes/{id}")]
    Task<Recipe> GetRecipeAsync(int id);

    [Get("/recipes")]
    Task<List<Recipe>> GetRecipesBatchAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    [Get("/recipes/search")]
    Task<List<int>> GetRecipesByOutputAsync([AliasAs("output")] int itemId);

    [Get("/recipes/search")]
    Task<List<int>> GetRecipesByInputAsync([AliasAs("input")] int itemId);

    // ── Commerce ───────────────────────────────────────────────────────────────
    [Get("/commerce/prices/{id}")]
    Task<CommercePrices> GetItemPricesAsync(int id);

    [Get("/commerce/prices")]
    Task<List<CommercePrices>> GetItemPricesBatchAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    [Get("/commerce/listings/{id}")]
    Task<CommerceListings> GetItemListingsAsync(int id);

    [Get("/commerce/exchange/coins")]
    Task<ExchangeRate> GetCoinToGemRateAsync([AliasAs("quantity")] long coins);

    [Get("/commerce/exchange/gems")]
    Task<ExchangeRate> GetGemToCoinRateAsync([AliasAs("quantity")] int gems);

    [Get("/commerce/delivery")]
    Task<CommerceDelivery> GetDeliveryAsync();

    // ── Worlds ─────────────────────────────────────────────────────────────────
    [Get("/worlds/{id}")]
    Task<World> GetWorldAsync(int id);

    // ── WvW ────────────────────────────────────────────────────────────────────
    [Get("/wvw/matches?ids=all")]
    Task<List<WvwMatch>> GetWvwMatchesAsync();

    [Get("/wvw/matches")]
    Task<WvwMatch> GetWvwMatchByWorldAsync([AliasAs("world")] int worldId);

    // ── PvP ────────────────────────────────────────────────────────────────────
    [Get("/pvp/stats")]
    Task<PvpStats> GetPvpStatsAsync();

    [Get("/pvp/standings")]
    Task<List<PvpStanding>> GetPvpStandingsAsync();

    // ── Skills / Specializations / Traits ──────────────────────────────────────
    [Get("/skills")]
    Task<List<Skill>> GetSkillsAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    [Get("/specializations")]
    Task<List<Specialization>> GetSpecializationsAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);

    [Get("/traits")]
    Task<List<Trait>> GetTraitsAsync([Query(CollectionFormat.Csv)] IEnumerable<int> ids);
}
