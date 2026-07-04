using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class AuthCommands
{
    public static Command Build(ConfigService config)
    {
        var auth = new Command("auth", "Manage API key authentication");
        auth.AddCommand(BuildSet(config));
        auth.AddCommand(BuildShow(config));
        auth.AddCommand(BuildClear(config));
        return auth;
    }

    private static Command BuildSet(ConfigService config)
    {
        var keyArg = new Argument<string>("api-key", "Your GW2 API key (generate at account.arena.net)");
        var cmd = new Command("set", "Store API key locally");
        cmd.AddArgument(keyArg);
        cmd.SetHandler((InvocationContext ctx) =>
        {
            var key = ctx.ParseResult.GetValueForArgument(keyArg);
            config.SetApiKey(key);
            AnsiConsole.MarkupLine("[green]API key saved.[/] Config: [grey]~/.gw2cli/config.json[/]");
        });
        return cmd;
    }

    private static Command BuildShow(ConfigService config)
    {
        var cmd = new Command("show", "Display the stored API key (masked)");
        cmd.SetHandler(() =>
        {
            var key = config.ApiKey;
            if (key is null)
            {
                AnsiConsole.MarkupLine("[yellow]No API key stored.[/] Use [bold]gw2 auth set <key>[/] to configure.");
                return;
            }
            var masked = key.Length > 8
                ? key[..4] + new string('*', key.Length - 8) + key[^4..]
                : new string('*', key.Length);
            AnsiConsole.MarkupLine($"API key: [cyan]{Markup.Escape(masked)}[/]");
        });
        return cmd;
    }

    private static Command BuildClear(ConfigService config)
    {
        var cmd = new Command("clear", "Remove the stored API key");
        cmd.SetHandler(() =>
        {
            config.ClearApiKey();
            AnsiConsole.MarkupLine("[green]API key removed.[/]");
        });
        return cmd;
    }
}
