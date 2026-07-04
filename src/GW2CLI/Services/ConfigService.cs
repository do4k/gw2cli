using System.Text.Json;

namespace GW2CLI.Services;

public class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gw2cli");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    private GW2Config _config;

    public ConfigService()
    {
        _config = Load();
    }

    public string? ApiKey => _config.ApiKey;

    public void SetApiKey(string key)
    {
        _config = _config with { ApiKey = key };
        Save();
    }

    public void ClearApiKey()
    {
        _config = _config with { ApiKey = null };
        Save();
    }

    private GW2Config Load()
    {
        if (!File.Exists(ConfigFile))
            return new GW2Config();
        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<GW2Config>(json) ?? new GW2Config();
        }
        catch
        {
            return new GW2Config();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
    }
}

public record GW2Config(string? ApiKey = null);
