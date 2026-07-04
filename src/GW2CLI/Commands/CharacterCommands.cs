using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class CharacterCommands
{
    public static Command Build(ConfigService config, GW2ApiService api, Option<string?> apiKeyOption)
    {
        var characters = new Command("characters", "List and inspect characters");

        var nameArg = new Argument<string?>("name", () => null, "Character name (omit to list all)");
        characters.AddArgument(nameArg);

        characters.AddCommand(BuildEquipment(api, apiKeyOption));
        characters.AddCommand(BuildSkills(api, apiKeyOption));
        characters.AddCommand(BuildCrafting(api, apiKeyOption));
        characters.AddCommand(BuildInventory(api, apiKeyOption));

        characters.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            if (name is null)
            {
                await Helpers.RunAsync(async () =>
                {
                    var names = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Fetching characters...", _ => api.GetCharacterNamesAsync());

                    var table = Helpers.NewTable("Character");
                    foreach (var n in names)
                        table.AddRow(Markup.Escape(n));
                    AnsiConsole.Write(table);
                });
            }
            else
            {
                await ShowCharacterSummary(api, name);
            }
        });

        return characters;
    }

    private static async Task ShowCharacterSummary(GW2ApiService api, string name)
    {
        await Helpers.RunAsync(async () =>
        {
            var c = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Fetching {name}...", _ => api.GetCharacterAsync(name));

            var grid = new Grid().AddColumn().AddColumn();
            void Row(string label, string value)
                => grid.AddRow($"[grey]{label}[/]", value);

            Row("Race", c.Race);
            Row("Gender", c.Gender);
            Row("Profession", Helpers.ProfessionMarkup(c.Profession));
            Row("Level", c.Level.ToString());
            Row("Playtime", Helpers.FormatPlaytime(c.Age));
            Row("Deaths", c.Deaths.ToString("N0"));
            Row("Experience", c.Experience.ToString("N0"));
            Row("Created", c.Created.ToString("yyyy-MM-dd"));

            if (c.Crafting.Count > 0)
            {
                var crafts = string.Join(", ", c.Crafting.Select(d =>
                    $"{d.Discipline} {d.Rating}{(d.Active ? "" : " [grey](inactive)[/]")}"));
                Row("Crafting", crafts);
            }

            var panel = new Panel(grid)
            {
                Header = new PanelHeader($"[bold]{Markup.Escape(c.Name)}[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Cyan1)
            };
            AnsiConsole.Write(panel);
        });
    }

    private static Command BuildEquipment(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var cmd = new Command("equipment", "Show equipped items for a character");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            await Helpers.RunAsync(async () =>
            {
                var (character, items) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync($"Fetching equipment...", async _ =>
                    {
                        var c = await api.GetCharacterAsync(name);
                        var equipped = c.Equipment ?? [];
                        var ids = equipped.Select(e => e.Id).Distinct();
                        var i = await api.GetItemsAsync(ids);
                        return (c, i);
                    });

                var itemMap = items.ToDictionary(i => i.Id);
                var equipment = (character.Equipment ?? [])
                    .Where(e => e.Location is null or "Equipped")
                    .OrderBy(e => SlotOrder(e.Slot))
                    .ToList();

                var table = Helpers.NewTable("Slot", "Item", "Rarity", "Level");
                foreach (var e in equipment)
                {
                    itemMap.TryGetValue(e.Id, out var item);
                    var itemName = item?.Name ?? $"Item #{e.Id}";
                    var rarity = item is not null ? Helpers.RarityMarkup(item.Rarity) : "";
                    var level = item?.Level.ToString() ?? "";
                    table.AddRow(
                        Markup.Escape(e.Slot),
                        Markup.Escape(itemName),
                        rarity,
                        level
                    );
                }

                AnsiConsole.MarkupLine($"[bold]{Markup.Escape(name)}[/] — {Helpers.ProfessionMarkup(character.Profession)}");
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildSkills(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var modeOpt = new Option<string>("--mode", () => "pve", "Game mode (pve/pvp/wvw)");
        var cmd = new Command("skills", "Show skill build for a character");
        cmd.AddArgument(nameArg);
        cmd.AddOption(modeOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);
            var mode = ctx.ParseResult.GetValueForOption(modeOpt)?.ToLower() ?? "pve";

            await Helpers.RunAsync(async () =>
            {
                var (character, skills) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching skills...", async _ =>
                    {
                        var c = await api.GetCharacterAsync(name);
                        var skillSet = mode switch
                        {
                            "pvp" => c.Skills?.Pvp,
                            "wvw" => c.Skills?.Wvw,
                            _ => c.Skills?.Pve
                        };

                        var ids = new List<int>();
                        if (skillSet?.Heal.HasValue == true) ids.Add(skillSet.Heal.Value);
                        ids.AddRange(skillSet?.Utilities.Where(u => u.HasValue).Select(u => u!.Value) ?? []);
                        if (skillSet?.Elite.HasValue == true) ids.Add(skillSet.Elite.Value);

                        var s = await api.GetSkillsAsync(ids.Distinct());
                        return (c, s);
                    });

                var skillMap = skills.ToDictionary(s => s.Id);
                var skillSet = mode switch
                {
                    "pvp" => character.Skills?.Pvp,
                    "wvw" => character.Skills?.Wvw,
                    _ => character.Skills?.Pve
                };

                AnsiConsole.MarkupLine($"[bold]{Markup.Escape(name)}[/] — [grey]{mode.ToUpper()} skills[/]");

                var table = Helpers.NewTable("Slot", "Skill", "Description");
                AddSkillRow(table, "Heal", skillSet?.Heal, skillMap);
                for (int i = 0; i < (skillSet?.Utilities.Count ?? 0); i++)
                    AddSkillRow(table, $"Utility {i + 1}", skillSet!.Utilities[i], skillMap);
                AddSkillRow(table, "Elite", skillSet?.Elite, skillMap);

                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static void AddSkillRow(Table table, string slot, int? skillId, Dictionary<int, Skill> skillMap)
    {
        if (skillId is null || skillId == 0) return;
        skillMap.TryGetValue(skillId.Value, out var skill);
        var name = skill?.Name ?? $"Skill #{skillId}";
        var desc = skill?.Description ?? "";
        if (desc.Length > 60) desc = desc[..57] + "...";
        table.AddRow(Markup.Escape(slot), Markup.Escape(name), Markup.Escape(desc));
    }

    private static Command BuildCrafting(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var cmd = new Command("crafting", "Show crafting disciplines for a character");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            await Helpers.RunAsync(async () =>
            {
                var character = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching character...", _ => api.GetCharacterAsync(name));

                if (character.Crafting.Count == 0)
                {
                    AnsiConsole.MarkupLine("[grey]No crafting disciplines.[/]");
                    return;
                }

                var table = Helpers.NewTable("Discipline", "Rating", "Max", "Active");
                foreach (var d in character.Crafting.OrderByDescending(x => x.Rating))
                {
                    var max = CraftingMax(d.Discipline);
                    var pct = max > 0 ? (double)d.Rating / max : 0;
                    var bar = ProgressBar(pct, 15);
                    table.AddRow(
                        Markup.Escape(d.Discipline),
                        d.Rating.ToString(),
                        max.ToString(),
                        d.Active ? "[green]Yes[/]" : "[grey]No[/]"
                    );
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildInventory(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var cmd = new Command("inventory", "Show character bag contents");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            await Helpers.RunAsync(async () =>
            {
                var (character, items) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching inventory...", async _ =>
                    {
                        var c = await api.GetCharacterAsync(name);
                        var allSlots = (c.Bags ?? []).SelectMany(b => b.Inventory ?? [])
                            .Where(s => s != null).Select(s => s!.Id).Distinct();
                        var bagIds = (c.Bags ?? []).Select(b => b.Id).Distinct();
                        var allIds = allSlots.Concat(bagIds).Distinct();
                        var i = await api.GetItemsAsync(allIds);
                        return (c, i);
                    });

                var itemMap = items.ToDictionary(i => i.Id);
                var table = Helpers.NewTable("Bag", "Item", "Count", "Rarity");

                int bagNum = 1;
                foreach (var bag in character.Bags ?? [])
                {
                    itemMap.TryGetValue(bag.Id, out var bagItem);
                    var bagName = bagItem?.Name ?? $"Bag #{bag.Id}";
                    table.AddRow($"[grey]── {Markup.Escape(bagName)} ({bag.Size} slots) ──[/]", "", "", "");

                    foreach (var slot in bag.Inventory.Where(s => s != null))
                    {
                        itemMap.TryGetValue(slot!.Id, out var item);
                        var itemName = item?.Name ?? $"Item #{slot.Id}";
                        var rarity = item is not null ? Helpers.RarityMarkup(item.Rarity) : "";
                        table.AddRow("", Markup.Escape(itemName), slot.Count.ToString(), rarity);
                    }
                    bagNum++;
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static string ProgressBar(double pct, int width)
    {
        var filled = (int)(pct * width);
        var bar = new string('█', filled) + new string('░', width - filled);
        var color = pct >= 1.0 ? "green3" : pct >= 0.5 ? "yellow3" : "grey50";
        return $"[{color}]{bar}[/]";
    }

    private static int CraftingMax(string discipline) => discipline switch
    {
        "Chef"           => 400,
        "Jeweler"        => 400,
        _                => 500
    };

    private static int SlotOrder(string slot) => slot switch
    {
        "Helm"           => 0,
        "Shoulders"      => 1,
        "Coat"           => 2,
        "Gloves"         => 3,
        "Leggings"       => 4,
        "Boots"          => 5,
        "Backpack"       => 6,
        "Accessory1"     => 7,
        "Accessory2"     => 8,
        "Amulet"         => 9,
        "Ring1"          => 10,
        "Ring2"          => 11,
        "WeaponA1"       => 12,
        "WeaponA2"       => 13,
        "WeaponB1"       => 14,
        "WeaponB2"       => 15,
        "Sickle"         => 16,
        "Axe"            => 17,
        "Pick"           => 18,
        _                => 99
    };

    private static void ApplyOverride(InvocationContext ctx, GW2ApiService api, Option<string?> opt)
    {
        var key = ctx.ParseResult.GetValueForOption(opt);
        if (key is not null) api.SetOverrideKey(key);
    }
}
