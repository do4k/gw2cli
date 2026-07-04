using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class ItemCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var items = new Command("items", "Look up game items by ID");
        items.AddCommand(BuildGet(api, keyContext, apiKeyOption));
        return items;
    }

    private static Command BuildGet(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var idArg = new Argument<int>("item-id", "Item ID");
        var cmd = new Command("get", "Show item details and trading post price");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var id = ctx.ParseResult.GetValueForArgument(idArg);

            await Helpers.RunAsync(async () =>
            {
                var (item, prices) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching item...", async _ =>
                    {
                        var i = await api.GetItemAsync(id);
                        CommercePrices? p = null;
                        try { p = await api.GetItemPricesAsync(id); } catch { }
                        return (i, p);
                    });

                var grid = new Grid().AddColumn().AddColumn();
                void Row(string label, string value) => grid.AddRow($"[grey]{label}[/]", value);

                Row("ID", item.Id.ToString());
                Row("Type", item.Type);
                Row("Rarity", Helpers.RarityMarkup(item.Rarity));
                Row("Required Level", item.Level.ToString());
                Row("Vendor Value", Helpers.FormatCoins(item.VendorValue));
                if (!string.IsNullOrEmpty(item.Description)) Row("Description", Markup.Escape(item.Description));
                if (item.Flags.Count > 0) Row("Flags", string.Join(", ", item.Flags));
                if (item.Restrictions.Count > 0) Row("Restrictions", string.Join(", ", item.Restrictions));
                if (prices is not null)
                {
                    Row("Buy Order", Helpers.FormatCoins(prices.Buys.UnitPrice));
                    Row("Sell Listing", Helpers.FormatCoins(prices.Sells.UnitPrice));
                }
                if (!string.IsNullOrEmpty(item.ChatLink)) Row("Chat Link", Markup.Escape(item.ChatLink));

                var panel = new Panel(grid)
                {
                    Header = new PanelHeader($"{Helpers.RarityMarkup(item.Rarity)} [bold]{Markup.Escape(item.Name)}[/]"),
                    Border = BoxBorder.Rounded
                };
                AnsiConsole.Write(panel);
            });
        });
        return cmd;
    }
}
