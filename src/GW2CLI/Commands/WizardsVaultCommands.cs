using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class WizardsVaultCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var vault = new Command("vault", "Wizard's Vault daily and weekly objectives");
        vault.AddCommand(BuildDaily(api, keyContext, apiKeyOption));
        vault.AddCommand(BuildWeekly(api, keyContext, apiKeyOption));
        return vault;
    }

    private static Command BuildDaily(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("daily", "Show today's Wizard's Vault objectives");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var response = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching daily vault...", _ => api.GetWizardsVaultDailyAsync());
                PrintVault("Daily Wizard's Vault", response);
            });
        });
        return cmd;
    }

    private static Command BuildWeekly(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("weekly", "Show this week's Wizard's Vault objectives");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var response = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching weekly vault...", _ => api.GetWizardsVaultWeeklyAsync());
                PrintVault("Weekly Wizard's Vault", response);
            });
        });
        return cmd;
    }

    private static void PrintVault(string title, WizardsVaultResponse r)
    {
        AnsiConsole.MarkupLine($"[bold yellow]{title}[/]");

        var metaBar = ProgressBar((double)r.MetaProgressCurrent / Math.Max(1, r.MetaProgressComplete), 20);
        AnsiConsole.MarkupLine($"[grey]Meta:[/] {metaBar} {r.MetaProgressCurrent}/{r.MetaProgressComplete}  " +
            $"[grey]Reward:[/] [mediumpurple1]{r.MetaRewardAstral} Astral Acclaim[/]{(r.MetaRewardClaimed ? " [green](claimed)[/]" : "")}");

        var claimedAcclaim = r.Objectives.Where(o => o.Claimed).Sum(o => o.Acclaim);
        var totalAcclaim = r.Objectives.Sum(o => o.Acclaim);
        AnsiConsole.MarkupLine($"[grey]Acclaim earned:[/] [bold]{claimedAcclaim}[/] / {totalAcclaim}");
        AnsiConsole.WriteLine();

        var table = Helpers.NewTable("", "Objective", "Track", "Progress", "Acclaim");
        table.Columns[0].Width(3);
        foreach (var obj in r.Objectives)
        {
            var status = obj.Claimed ? "[green]✓[/]" : "[yellow]○[/]";
            var progress = !obj.Claimed && obj.ProgressComplete > 1
                ? $"{obj.ProgressCurrent}/{obj.ProgressComplete}"
                : obj.Claimed ? "[green]Done[/]" : "";
            var trackColor = obj.Track switch
            {
                "PvE" => "green3", "PvP" => "red", "WvW" => "yellow3", "Fractals" => "cyan1", _ => "grey"
            };
            table.AddRow(status, Markup.Escape(obj.Title),
                $"[{trackColor}]{Markup.Escape(obj.Track)}[/]", progress, obj.Acclaim.ToString());
        }
        AnsiConsole.Write(table);
    }

    private static string ProgressBar(double pct, int width)
    {
        var filled = (int)(pct * width);
        var bar = new string('█', filled) + new string('░', width - filled);
        var color = pct >= 1.0 ? "green3" : pct >= 0.5 ? "yellow3" : "grey50";
        return $"[{color}]{bar}[/]";
    }
}
