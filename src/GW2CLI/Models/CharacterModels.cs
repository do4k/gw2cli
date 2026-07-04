namespace GW2CLI.Models;

public class Character
{
    public string Name { get; set; } = "";
    public string Race { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Profession { get; set; } = "";
    public int Level { get; set; }
    public string? Guild { get; set; }
    public DateTimeOffset Created { get; set; }
    public int Age { get; set; }
    public long Experience { get; set; }
    public int Deaths { get; set; }
    public List<CraftingDiscipline> Crafting { get; set; } = [];
    public List<EquipmentItem>? Equipment { get; set; }
    public CharacterSkills? Skills { get; set; }
    public CharacterSpecializations? Specializations { get; set; }
    public List<CharacterBag>? Bags { get; set; }
    public List<CharacterTraining>? Training { get; set; }
}

public class CraftingDiscipline
{
    public string Discipline { get; set; } = "";
    public int Rating { get; set; }
    public bool Active { get; set; }
}

public class EquipmentItem
{
    public int Id { get; set; }
    public string Slot { get; set; } = "";
    public int? Skin { get; set; }
    public List<int>? Dyes { get; set; }
    public List<int>? Upgrades { get; set; }
    public List<int>? Infusions { get; set; }
    public string? Binding { get; set; }
    public string? BoundTo { get; set; }
    public string? Location { get; set; }
    public EquipmentStats? Stats { get; set; }
}

public class EquipmentStats
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Dictionary<string, int> Attributes { get; set; } = [];
}

public class CharacterSkills
{
    public SkillSet? Pve { get; set; }
    public SkillSet? Pvp { get; set; }
    public SkillSet? Wvw { get; set; }
}

public class SkillSet
{
    public int? Heal { get; set; }
    public List<int?> Utilities { get; set; } = [];
    public int? Elite { get; set; }
    public List<int?>? Legends { get; set; }
}

public class CharacterSpecializations
{
    public List<SpecializationChoice>? Pve { get; set; }
    public List<SpecializationChoice>? Pvp { get; set; }
    public List<SpecializationChoice>? Wvw { get; set; }
}

public class SpecializationChoice
{
    public int? Id { get; set; }
    public List<int?> Traits { get; set; } = [];
}

public class CharacterBag
{
    public int Id { get; set; }
    public int Size { get; set; }
    public List<BagSlotItem?> Inventory { get; set; } = [];
}

public class BagSlotItem
{
    public int Id { get; set; }
    public int Count { get; set; }
    public int? Charges { get; set; }
    public string? Binding { get; set; }
    public string? BoundTo { get; set; }
}

public class CharacterTraining
{
    public int Id { get; set; }
    public int Done { get; set; }
}

public class Specialization
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Profession { get; set; } = "";
    public bool Elite { get; set; }
    public string? Icon { get; set; }
    public string? Background { get; set; }
    public List<int> MinorTraits { get; set; } = [];
    public List<int> MajorTraits { get; set; } = [];
}

public class Trait
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slot { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Icon { get; set; }
    public string Tier { get; set; } = "";
}

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Icon { get; set; }
    public string? ChatLink { get; set; }
    public string? Type { get; set; }
    public string? Slot { get; set; }
    public string? Professions { get; set; }
}
