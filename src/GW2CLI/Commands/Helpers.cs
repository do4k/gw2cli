using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using GW2CLI.Services;
using Refit;
using Spectre.Console;

namespace GW2CLI.Commands;

internal static class Helpers
{
    public static string FormatCoins(long copper)
    {
        if (copper == 0) return "[grey]0c[/]";
        var g = copper / 10000;
        var s = (copper % 10000) / 100;
        var c = copper % 100;
        var parts = new List<string>();
        if (g > 0) parts.Add($"[yellow]{g}g[/]");
        if (s > 0) parts.Add($"[grey]{s}s[/]");
        if (c > 0 || parts.Count == 0) parts.Add($"[red3_1]{c}c[/]");
        return string.Join(" ", parts);
    }

    public static string FormatPlaytime(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalDays >= 1
            ? $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m"
            : $"{ts.Hours}h {ts.Minutes}m";
    }

    public static string RarityMarkup(string rarity) => rarity switch
    {
        "Legendary"  => $"[mediumpurple1]{rarity}[/]",
        "Ascended"   => $"[deepskyblue1]{rarity}[/]",
        "Exotic"     => $"[darkorange]{rarity}[/]",
        "Rare"       => $"[yellow3]{rarity}[/]",
        "Masterwork" => $"[green3]{rarity}[/]",
        "Fine"       => $"[dodgerblue2]{rarity}[/]",
        "Basic"      => $"[white]{rarity}[/]",
        "Junk"       => $"[grey50]{rarity}[/]",
        _            => rarity
    };

    public static string ProfessionMarkup(string profession) => profession switch
    {
        "Guardian"     => $"[royalblue1]{profession}[/]",
        "Warrior"      => $"[yellow3]{profession}[/]",
        "Engineer"     => $"[darkorange3]{profession}[/]",
        "Ranger"       => $"[green3]{profession}[/]",
        "Thief"        => $"[grey70]{profession}[/]",
        "Elementalist" => $"[red]{profession}[/]",
        "Mesmer"       => $"[mediumpurple1]{profession}[/]",
        "Necromancer"  => $"[green]{profession}[/]",
        "Revenant"     => $"[darkred]{profession}[/]",
        _              => profession
    };

    public static async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ApiException ex)
        {
            var msg = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized    => "Invalid or missing API key. Run 'gw2 auth set <key>' to configure.",
                HttpStatusCode.Forbidden       => "API key lacks required permissions for this endpoint.",
                HttpStatusCode.NotFound        => "Resource not found.",
                HttpStatusCode.TooManyRequests => "Rate limit exceeded. Wait a moment and try again.",
                HttpStatusCode.ServiceUnavailable => "GW2 API is temporarily unavailable.",
                _ => $"API error {(int)ex.StatusCode}: {ex.Message}"
            };
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(msg)}");
            Environment.Exit(1);
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Network error:[/] {Markup.Escape(ex.Message)}");
            Environment.Exit(1);
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request timed out.");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Unexpected error:[/] {Markup.Escape(ex.Message)}");
            Environment.Exit(1);
        }
    }

    public static Table NewTable(params string[] columns)
    {
        var t = new Table().BorderColor(Color.Grey35).Border(TableBorder.Rounded);
        foreach (var col in columns)
            t.AddColumn(new TableColumn($"[bold]{Markup.Escape(col)}[/]"));
        return t;
    }

    public static void ApplyOverride(InvocationContext ctx, ApiKeyContext keyContext, Option<string?> opt)
    {
        keyContext.Override = ctx.ParseResult.GetValueForOption(opt);
    }
}
