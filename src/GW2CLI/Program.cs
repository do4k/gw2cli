using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using GW2CLI.Services;
using GW2CLI.Commands;
using Microsoft.Extensions.DependencyInjection;
using Refit;

// ── Dependency injection ────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddSingleton<ConfigService>();
services.AddSingleton<ApiKeyContext>();
services.AddTransient<ApiKeyHandler>();
services.AddTransient<CachingHandler>();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
};

services
    .AddRefitClient<IGW2ApiClient>(new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
    })
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://api.guildwars2.com/v2");
        c.Timeout = TimeSpan.FromSeconds(30);
        c.DefaultRequestHeaders.Add("User-Agent", "gw2cli/1.0");
    })
    .AddHttpMessageHandler<CachingHandler>()
    .AddHttpMessageHandler<ApiKeyHandler>();

services.AddTransient<GW2ApiService>();

var provider = services.BuildServiceProvider();

var config     = provider.GetRequiredService<ConfigService>();
var api        = provider.GetRequiredService<GW2ApiService>();
var keyContext = provider.GetRequiredService<ApiKeyContext>();

// ── Command tree ────────────────────────────────────────────────────────────
var root = new RootCommand("gw2 — Guild Wars 2 command-line interface");

var apiKeyOption = new Option<string?>(["--api-key", "-k"], "Override the stored API key for this request");
root.AddGlobalOption(apiKeyOption);

root.AddCommand(AuthCommands.Build(config));
root.AddCommand(AccountCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(CharacterCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(AchievementCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(CraftingCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(CommerceCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(ItemCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(MasteryCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(WizardsVaultCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(WvWCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(PvpCommands.Build(api, keyContext, apiKeyOption));
root.AddCommand(LegendaryCommands.Build(api, keyContext, apiKeyOption));

return await root.InvokeAsync(args);
