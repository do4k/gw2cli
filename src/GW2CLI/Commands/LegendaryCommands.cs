using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class LegendaryCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("legendary", "Show items in the Legendary Armory");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var (armory, items) = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching legendary armory...", async _ =>
                    {
                        var a = await api.GetLegendaryArmoryAsync();
                        var i = await api.GetItemsAsync(a.Select(x => x.Id).Distinct());
                        return (a, i);
                    });

                if (armory.Count == 0) { AnsiConsole.MarkupLine("[grey]No legendary items in armory.[/]"); return; }

                var itemMap = items.ToDictionary(i => i.Id);
                var table = Helpers.NewTable("Item", "Type", "Count");
                foreach (var entry in armory.OrderBy(e => itemMap.TryGetValue(e.Id, out var it) ? it.Name : ""))
                {
                    itemMap.TryGetValue(entry.Id, out var item);
                    var name = item is not null
                        ? $"{Helpers.RarityMarkup(item.Rarity)} {Markup.Escape(item.Name)}"
                        : $"Item #{entry.Id}";
                    table.AddRow(name, Markup.Escape(item?.Type ?? ""), entry.Count.ToString());
                }

                AnsiConsole.MarkupLine($"[bold]Legendary Armory[/] — {armory.Count} item(s)");
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }
}
