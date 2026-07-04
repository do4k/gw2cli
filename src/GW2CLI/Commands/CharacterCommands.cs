using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class CharacterCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var characters = new Command("characters", "List and inspect characters");
        var nameArg = new Argument<string?>("name", () => null, "Character name (omit to list all)");
        characters.AddArgument(nameArg);

        characters.AddCommand(BuildEquipment(api, keyContext, apiKeyOption));
        characters.AddCommand(BuildSkills(api, keyContext, apiKeyOption));
        characters.AddCommand(BuildCrafting(api, keyContext, apiKeyOption));
        characters.AddCommand(BuildInventory(api, keyContext, apiKeyOption));
        characters.AddCommand(BuildNetWorth(api, keyContext, apiKeyOption));

        characters.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            if (name is null)
            {
                await Helpers.RunAsync(async () =>
                {
                    var names = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Fetching characters...", _ => api.GetCharacterNamesAsync());

                    var characters = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Fetching character details...", _ =>
                            Task.WhenAll(names.Select(n => api.GetCharacterAsync(n))));

                    var table = Helpers.NewTable("Character", "Profession", "Race", "Level");
                    foreach (var c in characters.OrderBy(c => c.Name))
                        table.AddRow(
                            Markup.Escape(c.Name),
                            Helpers.ProfessionMarkup(c.Profession),
                            c.Race,
                            c.Level.ToString());
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
            void Row(string label, string value) => grid.AddRow($"[grey]{label}[/]", value);

            Row("Race", c.Race);
            Row("Gender", c.Gender);
            Row("Profession", Helpers.ProfessionMarkup(c.Profession));
            Row("Level", c.Level.ToString());
            Row("Playtime", Helpers.FormatPlaytime(c.Age));
            Row("Deaths", c.Deaths.ToString("N0"));
            if (c.Experience > 0)
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

    private static Command BuildEquipment(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var cmd = new Command("equipment", "Show equipped items for a character");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            await Helpers.RunAsync(async () =>
            {
                var (character, items) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching equipment...", async _ =>
                    {
                        var c = await api.GetCharacterAsync(name);
                        var ids = (c.Equipment ?? []).Select(e => e.Id).Distinct();
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
                    table.AddRow(
                        Markup.Escape(e.Slot),
                        Markup.Escape(item?.Name ?? $"Item #{e.Id}"),
                        item is not null ? Helpers.RarityMarkup(item.Rarity) : "",
                        item?.Level.ToString() ?? ""
                    );
                }

                AnsiConsole.MarkupLine($"[bold]{Markup.Escape(name)}[/] — {Helpers.ProfessionMarkup(character.Profession)}");
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildSkills(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var modeOpt = new Option<string>("--mode", () => "pve", "Game mode (pve/pvp/wvw)");
        var cmd = new Command("skills", "Show skill build for a character");
        cmd.AddArgument(nameArg);
        cmd.AddOption(modeOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
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
                            _     => c.Skills?.Pve
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
                    _     => character.Skills?.Pve
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
        var desc = skill?.Description ?? "";
        if (desc.Length > 60) desc = desc[..57] + "...";
        table.AddRow(Markup.Escape(slot), Markup.Escape(skill?.Name ?? $"#{skillId}"), Markup.Escape(desc));
    }

    private static Command BuildCrafting(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var cmd = new Command("crafting", "Show crafting disciplines for a character");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
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
                    table.AddRow(
                        Markup.Escape(d.Discipline),
                        d.Rating.ToString(),
                        CraftingMax(d.Discipline).ToString(),
                        d.Active ? "[green]Yes[/]" : "[grey]No[/]"
                    );
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildInventory(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var cmd = new Command("inventory", "Show character bag contents");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            await Helpers.RunAsync(async () =>
            {
                var (character, items) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching inventory...", async _ =>
                    {
                        var c = await api.GetCharacterAsync(name);
                        var slotIds = (c.Bags ?? []).SelectMany(b => b.Inventory ?? [])
                            .Where(s => s != null).Select(s => s!.Id);
                        var bagIds = (c.Bags ?? []).Select(b => b.Id);
                        var i = await api.GetItemsAsync(slotIds.Concat(bagIds).Distinct());
                        return (c, i);
                    });

                var itemMap = items.ToDictionary(i => i.Id);
                var table = Helpers.NewTable("Bag", "Item", "Count", "Rarity");

                foreach (var bag in character.Bags ?? [])
                {
                    itemMap.TryGetValue(bag.Id, out var bagItem);
                    table.AddRow($"[grey]── {Markup.Escape(bagItem?.Name ?? $"Bag #{bag.Id}")} ({bag.Size} slots) ──[/]", "", "", "");
                    foreach (var slot in bag.Inventory.Where(s => s != null))
                    {
                        itemMap.TryGetValue(slot!.Id, out var item);
                        table.AddRow(
                            "",
                            Markup.Escape(item?.Name ?? $"Item #{slot.Id}"),
                            slot.Count.ToString(),
                            item is not null ? Helpers.RarityMarkup(item.Rarity) : ""
                        );
                    }
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildNetWorth(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var nameArg = new Argument<string>("name", "Character name");
        var cmd = new Command("networth", "Estimate character net worth from equipment and bags (TP sell prices)");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var name = ctx.ParseResult.GetValueForArgument(nameArg);

            await Helpers.RunAsync(async () =>
            {
                Character character = null!;
                Dictionary<int, CommercePrices> priceMap = [];
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching character and prices...", async _ =>
                    {
                        var c = await api.GetCharacterAsync(name);
                        character = c;

                        var equipIds = (c.Equipment ?? []).Select(e => e.Id);
                        var bagItemIds = (c.Bags ?? [])
                            .SelectMany(b => b.Inventory)
                            .Where(s => s != null)
                            .Select(s => s!.Id);
                        var allIds = equipIds.Concat(bagItemIds).Distinct().ToList();

                        var pricesTask = api.GetItemPricesBatchAsync(allIds);
                        var prices = await pricesTask;
                        priceMap = prices.ToDictionary(p => p.Id);
                    });

                // Equipment
                long equipValue = 0;
                foreach (var e in (character.Equipment ?? []).Where(e => e.Location is null or "Equipped"))
                    if (priceMap.TryGetValue(e.Id, out var p)) equipValue += p.Sells.UnitPrice;

                // Bags contents
                long bagValue = 0;
                var bagItems = (character.Bags ?? [])
                    .SelectMany(b => b.Inventory)
                    .Where(s => s != null)
                    .GroupBy(s => s!.Id)
                    .Select(g => (Id: g.Key, Count: g.Sum(s => s!.Count)))
                    .ToList();

                foreach (var entry in bagItems)
                    if (priceMap.TryGetValue(entry.Id, out var p)) bagValue += (long)p.Sells.UnitPrice * entry.Count;

                long total = equipValue + bagValue;

                var grid = new Grid().AddColumn().AddColumn();
                void Row(string label, string value) => grid.AddRow($"[grey]{label}[/]", value);
                Row("Equipment", Helpers.FormatCoins(equipValue));
                Row("Bag contents", Helpers.FormatCoins(bagValue));
                Row("Total (pre-tax)", Helpers.FormatCoins(total));
                Row("Total (post 15% tax)", Helpers.FormatCoins((long)(total * 0.85)));

                var panel = new Panel(grid)
                {
                    Header = new PanelHeader($"[bold]{Markup.Escape(name)}[/] — Net Worth"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow)
                };
                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine("[grey]Based on lowest sell listings. Items with no TP listing excluded.[/]");
            });
        });
        return cmd;
    }

    private static int CraftingMax(string discipline) => discipline switch
    {
        "Chef" or "Jeweler" => 400,
        _ => 500
    };

    private static int SlotOrder(string slot) => slot switch
    {
        "Helm" => 0, "Shoulders" => 1, "Coat" => 2, "Gloves" => 3,
        "Leggings" => 4, "Boots" => 5, "Backpack" => 6,
        "Accessory1" => 7, "Accessory2" => 8, "Amulet" => 9,
        "Ring1" => 10, "Ring2" => 11,
        "WeaponA1" => 12, "WeaponA2" => 13, "WeaponB1" => 14, "WeaponB2" => 15,
        "Sickle" => 16, "Axe" => 17, "Pick" => 18,
        _ => 99
    };
}
