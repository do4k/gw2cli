using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class CommerceCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var commerce = new Command("commerce", "Trading post prices, listings, and currency exchange");
        commerce.AddCommand(BuildPrice(api, keyContext, apiKeyOption));
        commerce.AddCommand(BuildListings(api, keyContext, apiKeyOption));
        commerce.AddCommand(BuildExchange(api, keyContext, apiKeyOption));
        commerce.AddCommand(BuildDelivery(api, keyContext, apiKeyOption));
        return commerce;
    }

    private static Command BuildPrice(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var itemIdArg = new Argument<int>("item-id", "Item ID");
        var cmd = new Command("price", "Show best buy/sell prices for an item");
        cmd.AddArgument(itemIdArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var itemId = ctx.ParseResult.GetValueForArgument(itemIdArg);

            await Helpers.RunAsync(async () =>
            {
                var (item, prices) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching prices...", async _ =>
                    {
                        Item? it = null;
                        try { it = await api.GetItemAsync(itemId); } catch { }
                        var p = await api.GetItemPricesAsync(itemId);
                        return (it, p);
                    });

                var name = item is not null
                    ? $"{Helpers.RarityMarkup(item.Rarity)} [bold]{Markup.Escape(item.Name)}[/]"
                    : $"Item [bold]#{itemId}[/]";
                AnsiConsole.MarkupLine(name);
                AnsiConsole.WriteLine();

                var table = Helpers.NewTable("", "Unit Price", "Quantity");
                table.AddRow("[green]Highest Buy Order[/]", Helpers.FormatCoins(prices.Buys.UnitPrice), prices.Buys.Quantity.ToString("N0"));
                table.AddRow("[red]Lowest Sell Listing[/]", Helpers.FormatCoins(prices.Sells.UnitPrice), prices.Sells.Quantity.ToString("N0"));
                AnsiConsole.Write(table);

                var spread = prices.Sells.UnitPrice - prices.Buys.UnitPrice;
                var spreadPct = prices.Buys.UnitPrice > 0 ? (double)spread / prices.Sells.UnitPrice : 0;
                AnsiConsole.MarkupLine($"[grey]Spread:[/] {Helpers.FormatCoins(spread)} ({spreadPct:P1})");
                if (prices.Sells.UnitPrice > 0)
                    AnsiConsole.MarkupLine($"[grey]Sell after 15% tax:[/] {Helpers.FormatCoins((int)(prices.Buys.UnitPrice * 0.85))}");
            });
        });
        return cmd;
    }

    private static Command BuildListings(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var itemIdArg = new Argument<int>("item-id", "Item ID");
        var limitOpt = new Option<int>("--limit", () => 10, "Number of top listings to show per side");
        var cmd = new Command("listings", "Show buy and sell order book for an item");
        cmd.AddArgument(itemIdArg);
        cmd.AddOption(limitOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var itemId = ctx.ParseResult.GetValueForArgument(itemIdArg);
            var limit = ctx.ParseResult.GetValueForOption(limitOpt);

            await Helpers.RunAsync(async () =>
            {
                var (item, listings) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching order book...", async _ =>
                    {
                        Item? it = null;
                        try { it = await api.GetItemAsync(itemId); } catch { }
                        var l = await api.GetItemListingsAsync(itemId);
                        return (it, l);
                    });

                var name = item is not null
                    ? $"{Helpers.RarityMarkup(item.Rarity)} [bold]{Markup.Escape(item.Name)}[/]"
                    : $"Item [bold]#{itemId}[/]";
                AnsiConsole.MarkupLine(name);
                AnsiConsole.WriteLine();

                AnsiConsole.MarkupLine("[green bold]Buy Orders (highest first)[/]");
                var buyTable = Helpers.NewTable("Unit Price", "Quantity", "Listings");
                foreach (var b in listings.Buys.OrderByDescending(x => x.UnitPrice).Take(limit))
                    buyTable.AddRow(Helpers.FormatCoins(b.UnitPrice), b.Quantity.ToString("N0"), b.Listings.ToString());
                AnsiConsole.Write(buyTable);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red bold]Sell Listings (lowest first)[/]");
                var sellTable = Helpers.NewTable("Unit Price", "Quantity", "Listings");
                foreach (var s in listings.Sells.OrderBy(x => x.UnitPrice).Take(limit))
                    sellTable.AddRow(Helpers.FormatCoins(s.UnitPrice), s.Quantity.ToString("N0"), s.Listings.ToString());
                AnsiConsole.Write(sellTable);
            });
        });
        return cmd;
    }

    private static Command BuildExchange(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var exchange = new Command("exchange", "Convert between coins and gems");

        var coinsCmd = new Command("coins", "Convert coins to gems");
        var coinsArg = new Argument<long>("amount", "Copper coins (10000 = 1 gold)");
        coinsCmd.AddArgument(coinsArg);
        coinsCmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var amount = ctx.ParseResult.GetValueForArgument(coinsArg);
            await Helpers.RunAsync(async () =>
            {
                var rate = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching rate...", _ => api.GetCoinToGemRateAsync(amount));
                AnsiConsole.MarkupLine($"{Helpers.FormatCoins(amount)} → [mediumpurple1]{rate.Quantity} gems[/]");
                AnsiConsole.MarkupLine($"[grey]Rate: {Helpers.FormatCoins(rate.CoinsPerGem)} per gem[/]");
            });
        });

        var gemsCmd = new Command("gems", "Convert gems to coins");
        var gemsArg = new Argument<int>("amount", "Number of gems");
        gemsCmd.AddArgument(gemsArg);
        gemsCmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var amount = ctx.ParseResult.GetValueForArgument(gemsArg);
            await Helpers.RunAsync(async () =>
            {
                var rate = await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching rate...", _ => api.GetGemToCoinRateAsync(amount));
                AnsiConsole.MarkupLine($"[mediumpurple1]{amount} gems[/] → {Helpers.FormatCoins(rate.Quantity)}");
                AnsiConsole.MarkupLine($"[grey]Rate: {Helpers.FormatCoins(rate.CoinsPerGem)} per gem[/]");
            });
        });

        exchange.AddCommand(coinsCmd);
        exchange.AddCommand(gemsCmd);
        return exchange;
    }

    private static Command BuildDelivery(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var cmd = new Command("delivery", "Show pending trading post delivery box");
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            await Helpers.RunAsync(async () =>
            {
                var (delivery, items) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching delivery...", async _ =>
                    {
                        var d = await api.GetDeliveryAsync();
                        var i = await api.GetItemsAsync(d.Items.Select(x => x.Id).Distinct());
                        return (d, i);
                    });

                AnsiConsole.MarkupLine("[bold]Trading Post Delivery[/]");
                if (delivery.Coins > 0)
                    AnsiConsole.MarkupLine($"[grey]Coins:[/] {Helpers.FormatCoins(delivery.Coins)}");

                if (delivery.Items.Count == 0 && delivery.Coins == 0)
                {
                    AnsiConsole.MarkupLine("[grey]Delivery box is empty.[/]");
                    return;
                }

                if (delivery.Items.Count > 0)
                {
                    var itemMap = items.ToDictionary(i => i.Id);
                    var table = Helpers.NewTable("Item", "Count", "Rarity");
                    foreach (var entry in delivery.Items)
                    {
                        itemMap.TryGetValue(entry.Id, out var item);
                        table.AddRow(
                            Markup.Escape(item?.Name ?? $"Item #{entry.Id}"),
                            entry.Count.ToString(),
                            item is not null ? Helpers.RarityMarkup(item.Rarity) : ""
                        );
                    }
                    AnsiConsole.Write(table);
                }
            });
        });
        return cmd;
    }
}
