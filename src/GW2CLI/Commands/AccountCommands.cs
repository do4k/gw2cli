using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace GW2CLI.Commands;

public static class AccountCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var account = new Command("account", "View account information");
        account.AddCommand(BuildWallet(api, keyContext, apiKeyOption));
        account.AddCommand(BuildBank(api, keyContext, apiKeyOption));
        account.AddCommand(BuildInventory(api, keyContext, apiKeyOption));
        account.AddCommand(BuildAchievements(api, keyContext, apiKeyOption));
        account.AddCommand(BuildMounts(api, keyContext, apiKeyOption));

        account.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var acc = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching account...", _ => api.GetAccountAsync());

                World? world = null;
                try { world = await api.GetWorldAsync(acc.World); } catch { }

                var panel = new Panel(BuildAccountRenderable(acc, world))
                {
                    Header = new PanelHeader($"[bold yellow]{Markup.Escape(acc.Name)}[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow)
                };
                AnsiConsole.Write(panel);
            });
        });

        return account;
    }

    private static IRenderable BuildAccountRenderable(Account acc, World? world)
    {
        var grid = new Grid().AddColumn().AddColumn();

        void Row(string label, string value)
            => grid.AddRow($"[grey]{label}[/]", value);

        Row("World", world?.Name ?? acc.World.ToString());
        Row("Account Age", $"Created {acc.Created:yyyy-MM-dd}");
        Row("AP (Daily)", acc.DailyAp.ToString("N0"));
        Row("AP (Monthly)", acc.MonthlyAp.ToString("N0"));
        Row("WvW Rank", acc.WvwRank.ToString("N0"));
        Row("Fractal Level", acc.FractalLevel.ToString());
        Row("Commander", acc.Commander ? "[cyan]Yes[/]" : "No");
        Row("Guilds", acc.Guilds.Count.ToString());
        Row("Expansions", string.Join(", ", acc.Access.Where(a => a != "PlayForFree" && a != "GuildWars2")));

        return grid;
    }

    private static Command BuildWallet(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("wallet", "Show currency balances");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var (wallet, currencies) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching wallet...", async _ =>
                    {
                        var w = await api.GetWalletAsync();
                        var c = await api.GetCurrenciesAsync(w.Select(e => e.Id));
                        return (w, c);
                    });

                var currencyMap = currencies.ToDictionary(c => c.Id);
                var ordered = wallet
                    .OrderBy(e => currencyMap.TryGetValue(e.Id, out var cur) ? cur.Order : 999)
                    .ToList();

                var table = Helpers.NewTable("Currency", "Amount");
                foreach (var entry in ordered)
                {
                    var name = currencyMap.TryGetValue(entry.Id, out var cur) ? cur.Name : $"#{entry.Id}";
                    var value = entry.Id == 1
                        ? Helpers.FormatCoins(entry.Value)
                        : entry.Value.ToString("N0");
                    table.AddRow(Markup.Escape(name), value);
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildBank(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("bank", "Show bank contents");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var (slots, items) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching bank...", async _ =>
                    {
                        var s = await api.GetBankAsync();
                        var ids = s.Where(x => x != null).Select(x => x!.Id).Distinct();
                        var i = await api.GetItemsAsync(ids);
                        return (s, i);
                    });

                var itemMap = items.ToDictionary(i => i.Id);
                var table = Helpers.NewTable("Slot", "Item", "Count", "Rarity", "Binding");
                int slot = 0;
                foreach (var entry in slots)
                {
                    slot++;
                    if (entry is null) continue;
                    itemMap.TryGetValue(entry.Id, out var item);
                    table.AddRow(
                        slot.ToString(),
                        Markup.Escape(item?.Name ?? $"Item #{entry.Id}"),
                        entry.Count.ToString(),
                        item is not null ? Helpers.RarityMarkup(item.Rarity) : "",
                        Markup.Escape(entry.Binding ?? "")
                    );
                }
                AnsiConsole.MarkupLine($"[grey]Occupied slots: {slots.Count(s => s != null)} / {slots.Count}[/]");
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildInventory(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("inventory", "Show shared inventory slots");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var (slots, items) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching shared inventory...", async _ =>
                    {
                        var s = await api.GetSharedInventoryAsync();
                        var ids = s.Where(x => x != null).Select(x => x!.Id).Distinct();
                        var i = await api.GetItemsAsync(ids);
                        return (s, i);
                    });

                var itemMap = items.ToDictionary(i => i.Id);
                var table = Helpers.NewTable("Item", "Count", "Rarity");
                foreach (var entry in slots.Where(s => s != null))
                {
                    itemMap.TryGetValue(entry!.Id, out var item);
                    table.AddRow(
                        Markup.Escape(item?.Name ?? $"Item #{entry.Id}"),
                        entry.Count.ToString(),
                        item is not null ? Helpers.RarityMarkup(item.Rarity) : ""
                    );
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildAchievements(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var filterOpt = new Option<string?>("--category", "Filter by category name (partial match)");
        var inProgressOpt = new Option<bool>("--in-progress", "Show only in-progress achievements");

        var cmd = new Command("achievements", "Show achievement progress summary");
        cmd.AddOption(filterOpt);
        cmd.AddOption(inProgressOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var filter = ctx.ParseResult.GetValueForOption(filterOpt);
            var inProgress = ctx.ParseResult.GetValueForOption(inProgressOpt);

            await Helpers.RunAsync(async () =>
            {
                var (categories, accountAchievements) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching achievements...", async _ =>
                    {
                        var cats = await api.GetAchievementCategoriesAsync();
                        var accs = await api.GetAccountAchievementsAsync();
                        return (cats, accs);
                    });

                var accMap = accountAchievements.ToDictionary(a => a.Id);
                var filteredCats = (filter is not null
                    ? categories.Where(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    : categories)
                    .OrderBy(c => c.Order).ToList();

                var table = Helpers.NewTable("Category", "Done", "Total", "Progress");
                foreach (var cat in filteredCats)
                {
                    var done = cat.Achievements.Count(id => accMap.TryGetValue(id, out var a) && a.Done);
                    var total = cat.Achievements.Count;
                    if (total == 0) continue;
                    if (inProgress && (done == 0 || done == total)) continue;

                    var pct = total > 0 ? (double)done / total : 0;
                    table.AddRow(
                        Markup.Escape(cat.Name),
                        done.ToString(),
                        total.ToString(),
                        ProgressBar(pct, 20)
                    );
                }

                var totalDone = accountAchievements.Count(a => a.Done);
                AnsiConsole.MarkupLine($"[grey]Total achievements completed: [bold]{totalDone:N0}[/][/]");
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildMounts(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("mounts", "Show unlocked mounts");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var mounts = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching mounts...", _ => api.GetAccountMountsAsync());

                if (mounts.Count == 0)
                {
                    AnsiConsole.MarkupLine("[grey]No mounts unlocked.[/]");
                    return;
                }

                var table = Helpers.NewTable("Mount");
                foreach (var m in mounts.OrderBy(x => x))
                    table.AddRow(Markup.Escape(m));
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
        return $"[{color}]{bar}[/] {pct:P0}";
    }
}
