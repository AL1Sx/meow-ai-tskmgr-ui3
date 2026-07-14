using System;
using System.Text.Json;
using Windows.Storage;

namespace meow_ai_tskmgr_ui3.Helpers;

public static class SettingsHelper
{
    private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public static T? GetSetting<T>(string key)
    {
        try
        {
            if (_localSettings.Values.ContainsKey(key))
            {
                var value = _localSettings.Values[key];
                if (value is string json)
                {
                    return JsonSerializer.Deserialize<T>(json);
                }
                return (T)value;
            }
        }
        catch { }
        return default;
    }

    public static void SetSetting<T>(string key, T value)
    {
        try
        {
            if (value is string || value is int || value is bool || value is double)
            {
                _localSettings.Values[key] = value;
            }
            else
            {
                _localSettings.Values[key] = JsonSerializer.Serialize(value);
            }
        }
        catch { }
    }

    public static void RemoveSetting(string key)
    {
        try
        {
            _localSettings.Values.Remove(key);
        }
        catch { }
    }
}
