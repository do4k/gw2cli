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
    public List<int?>? Dyes { get; set; }
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

public class MasteryTrack
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Requirement { get; set; } = "";
    public int Order { get; set; }
    public string Background { get; set; } = "";
    public string Region { get; set; } = "";
    public List<MasteryLevel> Levels { get; set; } = [];
}

public class MasteryLevel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Instruction { get; set; } = "";
    public string Icon { get; set; } = "";
    public int PointCost { get; set; }
    public int ExpCost { get; set; }
}

public class LegendaryArmoryEntry
{
    public int Id { get; set; }
    public int Count { get; set; }
}

// Wizard's Vault
public class WizardsVaultResponse
{
    public int MetaProgressCurrent { get; set; }
    public int MetaProgressComplete { get; set; }
    public int MetaRewardItemId { get; set; }
    public int MetaRewardAstral { get; set; }
    public bool MetaRewardClaimed { get; set; }
    public List<WizardsVaultObjective> Objectives { get; set; } = [];
}

public class WizardsVaultObjective
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Track { get; set; } = "";
    public int Acclaim { get; set; }
    public int ProgressCurrent { get; set; }
    public int ProgressComplete { get; set; }
    public bool Claimed { get; set; }
}

// WvW
public class WvwMatch
{
    public string Id { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public WvwScores Scores { get; set; } = new();
    public WvwScores Kills { get; set; } = new();
    public WvwScores Deaths { get; set; } = new();
    public WvwWorlds Worlds { get; set; } = new();
    public List<WvwMap> Maps { get; set; } = [];
    public WvwSkirmish? Skirmish { get; set; }
}

public class WvwScores
{
    public int Red { get; set; }
    public int Blue { get; set; }
    public int Green { get; set; }
}

public class WvwWorlds
{
    public int Red { get; set; }
    public int Blue { get; set; }
    public int Green { get; set; }
}

public class WvwMap
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public WvwScores Scores { get; set; } = new();
    public WvwScores Kills { get; set; } = new();
    public WvwScores Deaths { get; set; } = new();
    public List<WvwObjective> Objectives { get; set; } = [];
}

public class WvwObjective
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Owner { get; set; } = "";
    public DateTimeOffset? LastFlipped { get; set; }
    public string? ClaimedBy { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public int PointsTick { get; set; }
    public int PointsCapture { get; set; }
}

public class WvwSkirmish
{
    public int Id { get; set; }
    public WvwScores Scores { get; set; } = new();
}

// PvP
public class PvpStats
{
    public int Pvprank { get; set; }
    public PvpAggregateStats Aggregate { get; set; } = new();
    public Dictionary<string, PvpAggregateStats> Professions { get; set; } = [];
    public Dictionary<string, PvpAggregateStats> Ladders { get; set; } = [];
}

public class PvpAggregateStats
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Desertions { get; set; }
    public int Byes { get; set; }
    public int Forfeits { get; set; }
}

public class PvpStanding
{
    public PvpCurrentStanding? Current { get; set; }
    public PvpCurrentStanding? Best { get; set; }
    public int SeasonId { get; set; }
}

public class PvpCurrentStanding
{
    public int TotalPointsCurrent { get; set; }
    public int Division { get; set; }
    public int Tier { get; set; }
    public int Points { get; set; }
    public int Repeats { get; set; }
    public int? Rating { get; set; }
    public int? Decay { get; set; }
}
