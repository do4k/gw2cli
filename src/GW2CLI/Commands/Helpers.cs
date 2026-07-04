using GW2CLI.Services;
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
        "Guardian"    => $"[royalblue1]{profession}[/]",
        "Warrior"     => $"[yellow3]{profession}[/]",
        "Engineer"    => $"[darkorange3]{profession}[/]",
        "Ranger"      => $"[green3]{profession}[/]",
        "Thief"       => $"[grey70]{profession}[/]",
        "Elementalist"=> $"[red]{profession}[/]",
        "Mesmer"      => $"[mediumpurple1]{profession}[/]",
        "Necromancer" => $"[green]{profession}[/]",
        "Revenant"    => $"[darkred]{profession}[/]",
        _             => profession
    };

    public static async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (GW2ApiException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
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
}
