namespace GW2CLI.Models;

public class Item
{
    public int Id { get; set; }
    public string? ChatLink { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public int Level { get; set; }
    public string Rarity { get; set; } = "";
    public int VendorValue { get; set; }
    public string? Icon { get; set; }
    public List<string> Flags { get; set; } = [];
    public List<string> GameTypes { get; set; } = [];
    public List<string> Restrictions { get; set; } = [];
}

public class Recipe
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public int OutputItemId { get; set; }
    public int OutputItemCount { get; set; }
    public int TimeToCraftMs { get; set; }
    public List<string> Disciplines { get; set; } = [];
    public int MinRating { get; set; }
    public List<string> Flags { get; set; } = [];
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<RecipeIngredient>? GuildIngredients { get; set; }
    public int? OutputUpgradeId { get; set; }
    public string? ChatLink { get; set; }
}

public class RecipeIngredient
{
    public int ItemId { get; set; }
    public int Count { get; set; }
}

public class CommercePrices
{
    public int Id { get; set; }
    public bool Whitelisted { get; set; }
    public PriceSummary Buys { get; set; } = new();
    public PriceSummary Sells { get; set; } = new();
}

public class PriceSummary
{
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
}

public class CommerceListings
{
    public int Id { get; set; }
    public List<ListingEntry> Buys { get; set; } = [];
    public List<ListingEntry> Sells { get; set; } = [];
}

public class ListingEntry
{
    public int Listings { get; set; }
    public int UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class ExchangeRate
{
    public int CoinsPerGem { get; set; }
    public long Quantity { get; set; }
}

public class CommerceDelivery
{
    public List<DeliveryItem> Items { get; set; } = [];
    public long Coins { get; set; }
}

public class DeliveryItem
{
    public int Id { get; set; }
    public int Count { get; set; }
}
