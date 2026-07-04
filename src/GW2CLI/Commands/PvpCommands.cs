using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class PvpCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var pvp = new Command("pvp", "PvP stats and standings");
        pvp.AddCommand(BuildStats(api, keyContext, apiKeyOption));
        pvp.AddCommand(BuildStandings(api, keyContext, apiKeyOption));
        return pvp;
    }

    private static Command BuildStats(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("stats", "Show PvP win/loss record");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var stats = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching PvP stats...", _ => api.GetPvpStatsAsync());

                AnsiConsole.MarkupLine($"[bold]PvP Stats[/] — Rank {stats.Pvprank}");
                AnsiConsole.WriteLine();

                var table = Helpers.NewTable("Mode", "Wins", "Losses", "Desertions", "Win Rate");
                AddStatsRow(table, "Overall", stats.Aggregate);
                foreach (var (ladder, s) in stats.Ladders.OrderBy(kv => kv.Key))
                    AddStatsRow(table, Markup.Escape(ladder), s);
                AnsiConsole.Write(table);

                if (stats.Professions.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold]By Profession[/]");
                    var profTable = Helpers.NewTable("Profession", "Wins", "Losses", "Win Rate");
                    foreach (var (prof, s) in stats.Professions.OrderByDescending(kv => kv.Value.Wins))
                    {
                        var total = s.Wins + s.Losses;
                        var pct = total > 0 ? (double)s.Wins / total : 0;
                        profTable.AddRow(Helpers.ProfessionMarkup(prof), s.Wins.ToString(), s.Losses.ToString(), $"{pct:P1}");
                    }
                    AnsiConsole.Write(profTable);
                }
            });
        });
        return cmd;
    }

    private static Command BuildStandings(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("standings", "Show ranked PvP season standings");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var standings = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching standings...", _ => api.GetPvpStandingsAsync());

                if (standings.Count == 0) { AnsiConsole.MarkupLine("[grey]No PvP season standings found.[/]"); return; }

                var table = Helpers.NewTable("Season", "Division", "Tier", "Points", "Rating", "Best");
                foreach (var s in standings)
                    table.AddRow(s.SeasonId.ToString(), s.Current?.Division.ToString() ?? "—",
                        s.Current?.Tier.ToString() ?? "—", s.Current?.Points.ToString() ?? "—",
                        s.Current?.Rating?.ToString() ?? "—", s.Best?.Division.ToString() ?? "—");
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static void AddStatsRow(Table table, string label, PvpAggregateStats s)
    {
        var total = s.Wins + s.Losses;
        var pct = total > 0 ? (double)s.Wins / total : 0;
        table.AddRow(label, s.Wins.ToString(), s.Losses.ToString(),
            s.Desertions > 0 ? s.Desertions.ToString() : "—",
            $"[{(pct >= 0.5 ? "green3" : "red")}]{pct:P1}[/]");
    }
}
