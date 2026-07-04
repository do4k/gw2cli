namespace GW2CLI.Models;

public class Account
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int World { get; set; }
    public List<string> Guilds { get; set; } = [];
    public List<string>? GuildLeader { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public List<string> Access { get; set; } = [];
    public bool Commander { get; set; }
    public int FractalLevel { get; set; }
    public int DailyAp { get; set; }
    public int MonthlyAp { get; set; }
    public int WvwRank { get; set; }
}

public class WalletEntry
{
    public int Id { get; set; }
    public long Value { get; set; }
}

public class Currency
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Order { get; set; }
    public string Icon { get; set; } = "";
}

public class BankSlot
{
    public int Id { get; set; }
    public int Count { get; set; }
    public int? Charges { get; set; }
    public int? Skin { get; set; }
    public List<int>? Dyes { get; set; }
    public List<int>? Upgrades { get; set; }
    public List<int>? Infusions { get; set; }
    public string? Binding { get; set; }
    public string? BoundTo { get; set; }
}

public class InventorySlot
{
    public int Id { get; set; }
    public int Count { get; set; }
    public int? Charges { get; set; }
    public string? Binding { get; set; }
    public string? BoundTo { get; set; }
}

public class AccountAchievement
{
    public int Id { get; set; }
    public int? Current { get; set; }
    public int? Max { get; set; }
    public bool Done { get; set; }
    public List<int>? Bits { get; set; }
    public int? Repeated { get; set; }
    public bool Unlocked { get; set; } = true;
}

public class World
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Population { get; set; } = "";
}

public class AccountMastery
{
    public int Id { get; set; }
    public int Level { get; set; }
}
