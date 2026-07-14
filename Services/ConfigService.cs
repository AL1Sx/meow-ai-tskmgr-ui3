using System;
using System.IO;
using System.Text.Json;
using meow_ai_tskmgr_ui3.Models;

namespace meow_ai_tskmgr_ui3.Services;

public class ConfigService
{
    private readonly string _configPath;
    private AppConfig _config;

    public ConfigService()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "Assets", "appsettings.json");
        _config = LoadConfig();
    }

    public AppConfig Config => _config;

    private AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    public void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch { }
    }

    public void UpdateConfig(Action<AppConfig> updater)
    {
        updater(_config);
        SaveConfig();
    }
}
