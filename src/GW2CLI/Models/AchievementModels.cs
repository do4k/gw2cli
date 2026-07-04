namespace GW2CLI.Models;

public class Achievement
{
    public int Id { get; set; }
    public string? Icon { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Requirement { get; set; } = "";
    public string LockedText { get; set; } = "";
    public string Type { get; set; } = "";
    public List<string> Flags { get; set; } = [];
    public List<AchievementTier> Tiers { get; set; } = [];
    public List<AchievementReward>? Rewards { get; set; }
    public List<AchievementBit>? Bits { get; set; }
    public int? PointCap { get; set; }
}

public class AchievementTier
{
    public int Count { get; set; }
    public int Points { get; set; }
}

public class AchievementReward
{
    public string Type { get; set; } = "";
    public int? Id { get; set; }
    public int? Count { get; set; }
    public string? Region { get; set; }
}

public class AchievementBit
{
    public string Type { get; set; } = "";
    public int? Id { get; set; }
    public string? Text { get; set; }
}

public class DailyAchievements
{
    public List<DailyEntry> Pve { get; set; } = [];
    public List<DailyEntry> Pvp { get; set; } = [];
    public List<DailyEntry> Wvw { get; set; } = [];
    public List<DailyEntry> Fractals { get; set; } = [];
    public List<DailyEntry> Special { get; set; } = [];
}

public class DailyEntry
{
    public int Id { get; set; }
    public DailyLevel Level { get; set; } = new();
    public List<string> RequiredAccess { get; set; } = [];
}

public class DailyLevel
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class AchievementCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Order { get; set; }
    public string? Icon { get; set; }
    public List<int> Achievements { get; set; } = [];
}

public class AchievementGroup
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Order { get; set; }
    public List<string> Categories { get; set; } = [];
}
