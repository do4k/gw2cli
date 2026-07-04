using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class CraftingCommands
{
    public static Command Build(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var crafting = new Command("crafting", "Search and inspect crafting recipes");
        crafting.AddCommand(BuildRecipe(api, keyContext, apiKeyOption));
        crafting.AddCommand(BuildSearch(api, keyContext, apiKeyOption));
        crafting.AddCommand(BuildUsedIn(api, keyContext, apiKeyOption));
        return crafting;
    }

    private static Command BuildRecipe(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var idArg = new Argument<int>("recipe-id", "Recipe ID");
        var cmd = new Command("recipe", "Show recipe details with ingredients and costs");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var id = ctx.ParseResult.GetValueForArgument(idArg);

            await Helpers.RunAsync(async () =>
            {
                var (recipe, items, prices) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching recipe...", async _ =>
                    {
                        var r = await api.GetRecipeAsync(id);
                        var allIds = r.Ingredients.Select(i => i.ItemId).Append(r.OutputItemId).Distinct().ToList();
                        var i = await api.GetItemsAsync(allIds);
                        List<CommercePrices>? p = null;
                        try { p = await api.GetItemPricesBatchAsync(allIds); } catch { }
                        return (r, i, p);
                    });

                var itemMap = items.ToDictionary(i => i.Id);
                var priceMap = prices?.ToDictionary(p => p.Id) ?? [];

                itemMap.TryGetValue(recipe.OutputItemId, out var output);
                var outName = output?.Name ?? $"Item #{recipe.OutputItemId}";
                var outRarity = output is not null ? Helpers.RarityMarkup(output.Rarity) : "";

                AnsiConsole.MarkupLine($"[bold]Recipe:[/] {outRarity} {Markup.Escape(outName)} x{recipe.OutputItemCount}");
                AnsiConsole.MarkupLine($"[grey]Disciplines:[/] {string.Join(", ", recipe.Disciplines)} (Rating {recipe.MinRating}+)");
                AnsiConsole.MarkupLine($"[grey]Craft time:[/] {recipe.TimeToCraftMs / 1000.0:F1}s");
                if (recipe.Flags.Contains("AutoLearned"))
                    AnsiConsole.MarkupLine("[grey]Auto-learned (no recipe sheet needed)[/]");
                AnsiConsole.WriteLine();

                var table = Helpers.NewTable("Ingredient", "Count", "Rarity", "Buy Price", "Sell Price");
                long totalBuyCost = 0;

                foreach (var ing in recipe.Ingredients)
                {
                    itemMap.TryGetValue(ing.ItemId, out var item);
                    priceMap.TryGetValue(ing.ItemId, out var price);
                    if (price != null) totalBuyCost += (long)price.Sells.UnitPrice * ing.Count;
                    table.AddRow(
                        Markup.Escape(item?.Name ?? $"Item #{ing.ItemId}"),
                        ing.Count.ToString(),
                        item is not null ? Helpers.RarityMarkup(item.Rarity) : "",
                        price is not null ? Helpers.FormatCoins(price.Sells.UnitPrice) : "[grey]—[/]",
                        price is not null ? Helpers.FormatCoins(price.Buys.UnitPrice) : "[grey]—[/]"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                if (totalBuyCost > 0)
                {
                    AnsiConsole.Markup($"[grey]Material cost (buy orders):[/] {Helpers.FormatCoins(totalBuyCost)}");
                    if (priceMap.TryGetValue(recipe.OutputItemId, out var outPrice) && outPrice.Buys.UnitPrice > 0)
                    {
                        var profit = (long)outPrice.Buys.UnitPrice * recipe.OutputItemCount - totalBuyCost;
                        var profitColor = profit >= 0 ? "green3" : "red";
                        AnsiConsole.MarkupLine($"  →  Output: {Helpers.FormatCoins(outPrice.Buys.UnitPrice)}  Profit: [{profitColor}]{Helpers.FormatCoins(profit)}[/]");
                    }
                    else AnsiConsole.WriteLine();
                }
            });
        });
        return cmd;
    }

    private static Command BuildSearch(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var itemIdArg = new Argument<int>("item-id", "Item ID to find recipes for");
        var cmd = new Command("search", "Find all recipes that produce a given item ID");
        cmd.AddArgument(itemIdArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var itemId = ctx.ParseResult.GetValueForArgument(itemIdArg);

            await Helpers.RunAsync(async () =>
            {
                var (item, recipes) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Searching recipes...", async _ =>
                    {
                        Item? it = null;
                        try { it = await api.GetItemAsync(itemId); } catch { }
                        var ids = await api.GetRecipesByOutputAsync(itemId);
                        var r = await api.GetRecipesAsync(ids);
                        return (it, r);
                    });

                if (item is not null)
                    AnsiConsole.MarkupLine($"Recipes producing: {Helpers.RarityMarkup(item.Rarity)} [bold]{Markup.Escape(item.Name)}[/]");
                else
                    AnsiConsole.MarkupLine($"Recipes producing item [bold]#{itemId}[/]");

                if (recipes.Count == 0) { AnsiConsole.MarkupLine("[grey]No recipes found.[/]"); return; }

                var ingIds = recipes.SelectMany(r => r.Ingredients.Select(i => i.ItemId)).Distinct();
                var ingredients = await api.GetItemsAsync(ingIds);
                var ingMap = ingredients.ToDictionary(i => i.Id);

                var table = Helpers.NewTable("Recipe ID", "Output Count", "Disciplines", "Rating", "Ingredients");
                foreach (var r in recipes)
                {
                    var ingList = string.Join(", ", r.Ingredients.Select(i =>
                    {
                        ingMap.TryGetValue(i.ItemId, out var ing);
                        return $"{ing?.Name ?? $"#{i.ItemId}"} x{i.Count}";
                    }));
                    table.AddRow(r.Id.ToString(), r.OutputItemCount.ToString(),
                        string.Join(", ", r.Disciplines), r.MinRating.ToString(), Markup.Escape(ingList));
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildUsedIn(GW2ApiService api, ApiKeyContext keyContext, Option<string?> apiKeyOption)
    {
        var itemIdArg = new Argument<int>("item-id", "Item ID to find as ingredient");
        var cmd = new Command("used-in", "Find recipes that use a given item ID as an ingredient");
        cmd.AddArgument(itemIdArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            Helpers.ApplyOverride(ctx, keyContext, apiKeyOption);
            var itemId = ctx.ParseResult.GetValueForArgument(itemIdArg);

            await Helpers.RunAsync(async () =>
            {
                var (item, recipes) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Searching recipes...", async _ =>
                    {
                        Item? it = null;
                        try { it = await api.GetItemAsync(itemId); } catch { }
                        var ids = await api.GetRecipesByInputAsync(itemId);
                        var r = await api.GetRecipesAsync(ids);
                        return (it, r);
                    });

                if (item is not null)
                    AnsiConsole.MarkupLine($"Recipes using: [bold]{Markup.Escape(item.Name)}[/] as ingredient");
                else
                    AnsiConsole.MarkupLine($"Recipes using item [bold]#{itemId}[/] as ingredient");

                if (recipes.Count == 0) { AnsiConsole.MarkupLine("[grey]No recipes found.[/]"); return; }

                var outputs = await api.GetItemsAsync(recipes.Select(r => r.OutputItemId).Distinct());
                var outMap = outputs.ToDictionary(i => i.Id);

                var table = Helpers.NewTable("Recipe ID", "Produces", "Count", "Disciplines", "Rating");
                foreach (var r in recipes)
                {
                    outMap.TryGetValue(r.OutputItemId, out var outItem);
                    var outName = outItem is not null
                        ? $"{Helpers.RarityMarkup(outItem.Rarity)} {Markup.Escape(outItem.Name)}"
                        : $"#{r.OutputItemId}";
                    table.AddRow(r.Id.ToString(), outName, r.OutputItemCount.ToString(),
                        string.Join(", ", r.Disciplines), r.MinRating.ToString());
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }
}
