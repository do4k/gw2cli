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
                var objectives = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching daily vault...", _ => api.GetWizardsVaultDailyAsync());
                PrintObjectives("Daily Wizard's Vault", objectives);
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
                var objectives = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching weekly vault...", _ => api.GetWizardsVaultWeeklyAsync());
                PrintObjectives("Weekly Wizard's Vault", objectives);
            });
        });
        return cmd;
    }

    private static void PrintObjectives(string title, List<WizardsVaultObjective> objectives)
    {
        AnsiConsole.MarkupLine($"[bold yellow]{title}[/]");
        var claimedAcclaim = objectives.Where(o => o.Claimed).Sum(o => o.AstralAcclaim);
        var totalAcclaim = objectives.Sum(o => o.AstralAcclaim);
        AnsiConsole.MarkupLine($"[grey]Astral Acclaim: [bold]{claimedAcclaim}[/] / {totalAcclaim}[/]");
        AnsiConsole.WriteLine();

        var table = Helpers.NewTable("", "Objective", "Track", "Progress", "Acclaim");
        table.Columns[0].Width(3);
        foreach (var obj in objectives)
        {
            var status = obj.Claimed ? "[green]✓[/]" : "[yellow]○[/]";
            var progress = obj.Current.HasValue && obj.Max.HasValue && !obj.Claimed
                ? $"{obj.Current}/{obj.Max}" : "";
            var trackColor = obj.Track switch
            {
                "PvE" => "green3", "PvP" => "red", "WvW" => "yellow3", "Fractals" => "cyan1", _ => "grey"
            };
            table.AddRow(status, Markup.Escape(obj.Title),
                $"[{trackColor}]{Markup.Escape(obj.Track)}[/]", progress, obj.AstralAcclaim.ToString());
        }
        AnsiConsole.Write(table);
    }
}
