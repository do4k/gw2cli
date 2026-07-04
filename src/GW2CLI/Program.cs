using System.CommandLine;
using GW2CLI.Services;
using GW2CLI.Commands;

var config = new ConfigService();
var api = new GW2ApiService(config);

var root = new RootCommand("gw2 — Guild Wars 2 command-line interface");

var apiKeyOption = new Option<string?>(["--api-key", "-k"], "Override the stored API key for this request");
root.AddGlobalOption(apiKeyOption);

root.AddCommand(AuthCommands.Build(config));
root.AddCommand(AccountCommands.Build(config, api, apiKeyOption));
root.AddCommand(CharacterCommands.Build(config, api, apiKeyOption));
root.AddCommand(AchievementCommands.Build(config, api, apiKeyOption));
root.AddCommand(CraftingCommands.Build(config, api, apiKeyOption));
root.AddCommand(CommerceCommands.Build(config, api, apiKeyOption));
root.AddCommand(ItemCommands.Build(config, api, apiKeyOption));

return await root.InvokeAsync(args);
