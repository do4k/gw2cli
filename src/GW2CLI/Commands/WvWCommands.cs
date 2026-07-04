using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class WvWCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var wvw = new Command("wvw", "World vs World match information");
        wvw.AddCommand(BuildMatch(api, keyContext, apiKeyOption));
        wvw.AddCommand(BuildAll(api, keyContext, apiKeyOption));
        return wvw;
    }

    private static Command BuildMatch(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("match", "Show your world's current WvW match (uses stored account world)");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var (account, match) = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching WvW match...", async _ =>
                    {
                        var acc = await api.GetAccountAsync();
                        var m = await api.GetWvwMatchByWorldAsync(acc.World);
                        return (acc, m);
                    });

                World? myWorld = null;
                try { myWorld = await api.GetWorldAsync(account.World); } catch { }
                var myTeam = match.Worlds.Red == account.World ? "Red"
                    : match.Worlds.Blue == account.World ? "Blue"
                    : match.Worlds.Green == account.World ? "Green" : "Unknown";

                AnsiConsole.MarkupLine($"[bold]WvW Match[/] — {Markup.Escape(match.Id)}");
                AnsiConsole.MarkupLine($"[grey]Your world:[/] {Markup.Escape(myWorld?.Name ?? account.World.ToString())} ({TeamMarkup(myTeam)})");
                AnsiConsole.MarkupLine($"[grey]Ends:[/] {match.EndTime:yyyy-MM-dd HH:mm} UTC");
                AnsiConsole.WriteLine();

                var scoreTable = Helpers.NewTable("Team", "World ID", "Score", "Kills", "Deaths");
                scoreTable.AddRow(TeamMarkup("Red"),   match.Worlds.Red.ToString(),   match.Scores.Red.ToString("N0"),   match.Kills.Red.ToString("N0"),   match.Deaths.Red.ToString("N0"));
                scoreTable.AddRow(TeamMarkup("Blue"),  match.Worlds.Blue.ToString(),  match.Scores.Blue.ToString("N0"),  match.Kills.Blue.ToString("N0"),  match.Deaths.Blue.ToString("N0"));
                scoreTable.AddRow(TeamMarkup("Green"), match.Worlds.Green.ToString(), match.Scores.Green.ToString("N0"), match.Kills.Green.ToString("N0"), match.Deaths.Green.ToString("N0"));
                AnsiConsole.MarkupLine("[bold]Scores[/]");
                AnsiConsole.Write(scoreTable);
                AnsiConsole.WriteLine();

                AnsiConsole.MarkupLine("[bold]By Map[/]");
                var mapTable = Helpers.NewTable("Map", "Red", "Blue", "Green");
                foreach (var map in match.Maps)
                    mapTable.AddRow(Markup.Escape(MapName(map.Type)), map.Scores.Red.ToString("N0"), map.Scores.Blue.ToString("N0"), map.Scores.Green.ToString("N0"));
                AnsiConsole.Write(mapTable);
            });
        });
        return cmd;
    }

    private static Command BuildAll(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("matches", "List all current WvW matches");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var matches = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching WvW matches...", _ => api.GetWvwMatchesAsync());
                var table = Helpers.NewTable("Match ID", "Red Score", "Blue Score", "Green Score", "Ends");
                foreach (var m in matches.OrderBy(m => m.Id))
                    table.AddRow(Markup.Escape(m.Id), m.Scores.Red.ToString("N0"), m.Scores.Blue.ToString("N0"), m.Scores.Green.ToString("N0"), m.EndTime.ToString("MM-dd HH:mm"));
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static string TeamMarkup(string team) => team switch
    {
        "Red" => "[red]Red[/]", "Blue" => "[blue]Blue[/]", "Green" => "[green]Green[/]", _ => team
    };

    private static string MapName(string type) => type switch
    {
        "RedHome" => "Red Borderlands", "BlueHome" => "Blue Borderlands",
        "GreenHome" => "Green Borderlands", "Center" => "Eternal Battlegrounds", _ => type
    };
}
