using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class AchievementCommands
{
    public static Command Build(ConfigService config, GW2ApiService api, Option<string?> apiKeyOption)
    {
        var achievements = new Command("achievements", "Browse and track achievements");
        achievements.AddCommand(BuildDaily(api, apiKeyOption));
        achievements.AddCommand(BuildGet(api, apiKeyOption));
        achievements.AddCommand(BuildCategories(api, apiKeyOption));
        return achievements;
    }

    private static Command BuildDaily(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var cmd = new Command("daily", "Show today's daily achievements with completion status");
        var noAuthOpt = new Option<bool>("--no-auth", "Skip account progress (no API key needed)");
        cmd.AddOption(noAuthOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var noAuth = ctx.ParseResult.GetValueForOption(noAuthOpt);

            await Helpers.RunAsync(async () =>
            {
                var (daily, achievements, accountProgress) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching daily achievements...", async _ =>
                    {
                        var d = await api.GetDailyAchievementsAsync();
                        var allIds = d.Pve.Concat(d.Pvp).Concat(d.Wvw).Concat(d.Fractals).Concat(d.Special)
                            .Select(e => e.Id).Distinct().ToList();
                        var a = await api.GetAchievementsAsync(allIds);

                        List<AccountAchievement>? progress = null;
                        if (!noAuth && api.EffectiveKey is not null)
                        {
                            try { progress = await api.GetAccountAchievementsByIdAsync(allIds); }
                            catch { }
                        }
                        return (d, a, progress);
                    });

                var achMap = achievements.ToDictionary(a => a.Id);
                var progressMap = accountProgress?.ToDictionary(a => a.Id) ?? [];

                AnsiConsole.MarkupLine($"[bold yellow]Daily Achievements — {DateTime.UtcNow:yyyy-MM-dd} UTC[/]");
                AnsiConsole.WriteLine();

                PrintDailySection("PvE", daily.Pve, achMap, progressMap);
                PrintDailySection("PvP", daily.Pvp, achMap, progressMap);
                PrintDailySection("WvW", daily.Wvw, achMap, progressMap);
                PrintDailySection("Fractals", daily.Fractals, achMap, progressMap);
                if (daily.Special.Count > 0)
                    PrintDailySection("Special", daily.Special, achMap, progressMap);
            });
        });
        return cmd;
    }

    private static void PrintDailySection(
        string section,
        List<DailyEntry> entries,
        Dictionary<int, Achievement> achMap,
        Dictionary<int, AccountAchievement> progressMap)
    {
        if (entries.Count == 0) return;

        AnsiConsole.MarkupLine($"[bold cyan]{section}[/]");
        var table = Helpers.NewTable("", "Achievement", "Requirement", "AP", "Status");
        table.Columns[0].Width(3);

        foreach (var entry in entries)
        {
            achMap.TryGetValue(entry.Id, out var ach);
            progressMap.TryGetValue(entry.Id, out var prog);

            var done = prog?.Done == true;
            var status = prog is null ? "[grey]?[/]" : done ? "[green]✓[/]" : "[yellow]○[/]";
            var icon = done ? "✓" : "○";
            var name = ach?.Name ?? $"Achievement #{entry.Id}";
            var req = ach?.Requirement ?? "";
            if (req.Length > 50) req = req[..47] + "...";
            var ap = ach?.Tiers.Sum(t => t.Points).ToString() ?? "";
            var levelRange = entry.Level.Min == 1 && entry.Level.Max == 80
                ? ""
                : $" [grey](L{entry.Level.Min}-{entry.Level.Max})[/]";

            table.AddRow(
                status,
                Markup.Escape(name) + levelRange,
                Markup.Escape(req),
                ap,
                ""
            );
        }
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static Command BuildGet(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var idArg = new Argument<int>("id", "Achievement ID");
        var cmd = new Command("get", "Show details for a specific achievement");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var id = ctx.ParseResult.GetValueForArgument(idArg);

            await Helpers.RunAsync(async () =>
            {
                var (achievements, progress) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching achievement...", async _ =>
                    {
                        var a = await api.GetAchievementsAsync([id]);
                        AccountAchievement? p = null;
                        if (api.EffectiveKey is not null)
                        {
                            try { p = (await api.GetAccountAchievementsByIdAsync([id])).FirstOrDefault(); }
                            catch { }
                        }
                        return (a, p);
                    });

                var ach = achievements.FirstOrDefault();
                if (ach is null)
                {
                    AnsiConsole.MarkupLine($"[red]Achievement {id} not found.[/]");
                    return;
                }

                var content = new Grid().AddColumn().AddColumn();
                void Row(string label, string value)
                    => content.AddRow($"[grey]{label}[/]", value);

                Row("Type", ach.Type);
                Row("Description", Markup.Escape(ach.Description));
                Row("Requirement", Markup.Escape(ach.Requirement));

                if (progress is not null)
                {
                    if (progress.Done)
                        Row("Status", "[green]Completed[/]");
                    else if (progress.Current.HasValue && progress.Max.HasValue)
                        Row("Progress", $"{progress.Current}/{progress.Max}");
                    if (progress.Repeated.HasValue && progress.Repeated > 0)
                        Row("Repeated", progress.Repeated.Value.ToString() + "x");
                }

                var totalAp = ach.Tiers.Sum(t => t.Points);
                Row("AP", totalAp.ToString());

                if (ach.Rewards?.Count > 0)
                {
                    var rewards = string.Join(", ", ach.Rewards.Select(r =>
                        r.Type == "Coins" ? Helpers.FormatCoins(r.Count ?? 0)
                        : r.Type == "Item" ? $"Item #{r.Id} x{r.Count}"
                        : r.Type == "Mastery" ? $"Mastery point ({r.Region})"
                        : r.Type == "Title" ? $"Title #{r.Id}"
                        : r.Type));
                    Row("Rewards", rewards);
                }

                if (ach.Bits?.Count > 0)
                {
                    var bits = string.Join("\n", ach.Bits.Take(10).Select(b =>
                        b.Type == "Text" ? $"  • {b.Text}" : $"  • {b.Type} #{b.Id}"));
                    Row("Objectives", Markup.Escape(bits));
                }

                var panel = new Panel(content)
                {
                    Header = new PanelHeader($"[bold]{Markup.Escape(ach.Name)}[/] [grey]#{ach.Id}[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow)
                };
                AnsiConsole.Write(panel);
            });
        });
        return cmd;
    }

    private static Command BuildCategories(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var cmd = new Command("categories", "List all achievement categories");

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var categories = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching categories...", _ => api.GetAchievementCategoriesAsync());

                var table = Helpers.NewTable("ID", "Category", "Achievements");
                foreach (var cat in categories.OrderBy(c => c.Order))
                    table.AddRow(cat.Id.ToString(), Markup.Escape(cat.Name), cat.Achievements.Count.ToString());
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static void ApplyOverride(InvocationContext ctx, GW2ApiService api, Option<string?> opt)
    {
        var key = ctx.ParseResult.GetValueForOption(opt);
        if (key is not null) api.SetOverrideKey(key);
    }
}
