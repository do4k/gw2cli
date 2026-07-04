using System.CommandLine;
using System.CommandLine.Invocation;
using GW2CLI.Services;
using GW2CLI.Models;
using Spectre.Console;

namespace GW2CLI.Commands;

public static class CraftingCommands
{
    public static Command Build(ConfigService config, GW2ApiService api, Option<string?> apiKeyOption)
    {
        var crafting = new Command("crafting", "Search and inspect crafting recipes");
        crafting.AddCommand(BuildRecipe(api, apiKeyOption));
        crafting.AddCommand(BuildSearch(api, apiKeyOption));
        crafting.AddCommand(BuildUsedIn(api, apiKeyOption));
        return crafting;
    }

    private static Command BuildRecipe(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var idArg = new Argument<int>("recipe-id", "Recipe ID");
        var cmd = new Command("recipe", "Show recipe details with ingredients and costs");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var id = ctx.ParseResult.GetValueForArgument(idArg);

            await Helpers.RunAsync(async () =>
            {
                var (recipe, items, prices) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching recipe...", async _ =>
                    {
                        var r = await api.GetRecipeAsync(id);
                        var allIds = r.Ingredients.Select(i => i.ItemId)
                            .Append(r.OutputItemId).Distinct().ToList();
                        var i = await api.GetItemsAsync(allIds);

                        List<CommercePrices>? p = null;
                        try { p = await api.GetItemPricesBatchAsync(allIds); } catch { }

                        return (r, i, p);
                    });

                var itemMap = items.ToDictionary(i => i.Id);
                var priceMap = prices?.ToDictionary(p => p.Id) ?? [];

                itemMap.TryGetValue(recipe.OutputItemId, out var output);
                var outputName = output?.Name ?? $"Item #{recipe.OutputItemId}";
                var outputRarity = output is not null ? Helpers.RarityMarkup(output.Rarity) : "";

                AnsiConsole.MarkupLine($"[bold]Recipe:[/] {outputRarity} {Markup.Escape(outputName)} x{recipe.OutputItemCount}");
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

                    var name = item?.Name ?? $"Item #{ing.ItemId}";
                    var rarity = item is not null ? Helpers.RarityMarkup(item.Rarity) : "";
                    var buyStr = price is not null ? Helpers.FormatCoins(price.Sells.UnitPrice) : "[grey]—[/]";
                    var sellStr = price is not null ? Helpers.FormatCoins(price.Buys.UnitPrice) : "[grey]—[/]";

                    if (price != null) totalBuyCost += (long)price.Sells.UnitPrice * ing.Count;

                    table.AddRow(
                        Markup.Escape(name),
                        ing.Count.ToString(),
                        rarity,
                        buyStr,
                        sellStr
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                if (totalBuyCost > 0)
                {
                    AnsiConsole.Markup($"[grey]Estimated material cost (buy orders):[/] {Helpers.FormatCoins(totalBuyCost)}");
                    if (priceMap.TryGetValue(recipe.OutputItemId, out var outPrice) && outPrice.Buys.UnitPrice > 0)
                    {
                        var profit = (long)outPrice.Buys.UnitPrice * recipe.OutputItemCount - totalBuyCost;
                        var profitColor = profit >= 0 ? "green3" : "red";
                        AnsiConsole.MarkupLine($"  →  Output sell price: {Helpers.FormatCoins(outPrice.Buys.UnitPrice)}  Profit: [{profitColor}]{Helpers.FormatCoins(profit)}[/]");
                    }
                    else
                    {
                        AnsiConsole.WriteLine();
                    }
                }
            });
        });
        return cmd;
    }

    private static Command BuildSearch(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var itemIdArg = new Argument<int>("item-id", "Item ID to find recipes for");
        var cmd = new Command("search", "Find all recipes that produce a given item ID");
        cmd.AddArgument(itemIdArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var itemId = ctx.ParseResult.GetValueForArgument(itemIdArg);

            await Helpers.RunAsync(async () =>
            {
                var (item, recipeIds, recipes) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Searching recipes...", async _ =>
                    {
                        Item? it = null;
                        try { it = await api.GetItemAsync(itemId); } catch { }
                        var ids = await api.GetRecipesByOutputAsync(itemId);
                        var r = await api.GetRecipesAsync(ids);
                        return (it, ids, r);
                    });

                if (item is not null)
                    AnsiConsole.MarkupLine($"Recipes producing: {Helpers.RarityMarkup(item.Rarity)} [bold]{Markup.Escape(item.Name)}[/]");
                else
                    AnsiConsole.MarkupLine($"Recipes producing item [bold]#{itemId}[/]");

                if (recipes.Count == 0)
                {
                    AnsiConsole.MarkupLine("[grey]No recipes found.[/]");
                    return;
                }

                var allIngredientIds = recipes.SelectMany(r => r.Ingredients.Select(i => i.ItemId)).Distinct();
                var ingredients = await api.GetItemsAsync(allIngredientIds);
                var ingMap = ingredients.ToDictionary(i => i.Id);

                var table = Helpers.NewTable("Recipe ID", "Output Count", "Disciplines", "Rating", "Ingredients");
                foreach (var r in recipes)
                {
                    var ingList = string.Join(", ", r.Ingredients.Select(i =>
                    {
                        ingMap.TryGetValue(i.ItemId, out var ing);
                        return $"{ing?.Name ?? $"#{i.ItemId}"} x{i.Count}";
                    }));
                    table.AddRow(
                        r.Id.ToString(),
                        r.OutputItemCount.ToString(),
                        string.Join(", ", r.Disciplines),
                        r.MinRating.ToString(),
                        Markup.Escape(ingList)
                    );
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static Command BuildUsedIn(GW2ApiService api, Option<string?> apiKeyOption)
    {
        var itemIdArg = new Argument<int>("item-id", "Item ID to find recipes that use it as ingredient");
        var cmd = new Command("used-in", "Find recipes that use a given item ID as an ingredient");
        cmd.AddArgument(itemIdArg);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ApplyOverride(ctx, api, apiKeyOption);
            var itemId = ctx.ParseResult.GetValueForArgument(itemIdArg);

            await Helpers.RunAsync(async () =>
            {
                var (item, recipeIds, recipes) = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Searching recipes...", async _ =>
                    {
                        Item? it = null;
                        try { it = await api.GetItemAsync(itemId); } catch { }
                        var ids = await api.GetRecipesByInputAsync(itemId);
                        var r = await api.GetRecipesAsync(ids);
                        return (it, ids, r);
                    });

                if (item is not null)
                    AnsiConsole.MarkupLine($"Recipes using: [bold]{Markup.Escape(item.Name)}[/] as ingredient");
                else
                    AnsiConsole.MarkupLine($"Recipes using item [bold]#{itemId}[/] as ingredient");

                if (recipes.Count == 0)
                {
                    AnsiConsole.MarkupLine("[grey]No recipes found.[/]");
                    return;
                }

                var outputIds = recipes.Select(r => r.OutputItemId).Distinct();
                var outputs = await api.GetItemsAsync(outputIds);
                var outMap = outputs.ToDictionary(i => i.Id);

                var table = Helpers.NewTable("Recipe ID", "Produces", "Count", "Disciplines", "Rating");
                foreach (var r in recipes)
                {
                    outMap.TryGetValue(r.OutputItemId, out var outItem);
                    var outName = outItem is not null
                        ? $"{Helpers.RarityMarkup(outItem.Rarity)} {Markup.Escape(outItem.Name)}"
                        : $"#{r.OutputItemId}";
                    table.AddRow(
                        r.Id.ToString(),
                        outName,
                        r.OutputItemCount.ToString(),
                        string.Join(", ", r.Disciplines),
                        r.MinRating.ToString()
                    );
                }
                AnsiConsole.Write(table);
            });
        });
        return cmd;
    }

    private static void ApplyOverride(InvocationContext ctx, GW2ApiService api, Option<string?> opt)
    {
        var key = ctx.ParseResult.GetValueForOption(opt);
        if (key is not null) api.SetOverrideKey(key);
    }
}
