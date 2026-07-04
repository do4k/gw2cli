using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class MasteryCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("masteries", "Show mastery track progress");
        var regionOpt = new Option<string?>("--region", "Filter by region (Tyria, Maguuma, CrystalDesert, Icebrood, EndOfDragons, SotO)");
        cmd.AddOption(regionOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var region = ctx.ParseResult.GetValueForOption(regionOpt);

            await Helpers.RunAsync(async () =>
            {
                var (accountMasteries, tracks) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching masteries...", async _ =>
                    {
                        var am = await api.GetAccountMasteriesAsync();
                        var t = await api.GetAllMasteryTracksAsync();
                        return (am, t);
                    });

                var masteryMap = accountMasteries.ToDictionary(m => m.Id);
                var filtered = (region is not null
                    ? tracks.Where(t => t.Region.Contains(region, StringComparison.OrdinalIgnoreCase))
                    : tracks)
                    .OrderBy(t => t.Region).ThenBy(t => t.Order).GroupBy(t => t.Region);

                foreach (var group in filtered)
                {
                    AnsiConsole.MarkupLine($"\n[bold cyan]{group.Key}[/]");
                    var table = Helpers.NewTable("Mastery", "Level", "Max", "Progress");
                    foreach (var track in group)
                    {
                        var maxLevel = track.Levels.Count;
                        masteryMap.TryGetValue(track.Id, out var entry);
                        var currentLevel = entry?.Level ?? 0;
                        var pct = maxLevel > 0 ? (double)currentLevel / maxLevel : 0;
                        var levelStr = currentLevel == maxLevel ? $"[green3]{currentLevel}[/]" : currentLevel.ToString();
                        table.AddRow(Markup.Escape(track.Name), levelStr, maxLevel.ToString(), ProgressBar(pct, 15));
                    }
                    AnsiConsole.Write(table);
                }
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
