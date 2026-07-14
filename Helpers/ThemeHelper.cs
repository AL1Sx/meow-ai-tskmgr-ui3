using System;
using Microsoft.UI.Xaml;

namespace meow_ai_tskmgr_ui3.Helpers;

public static class ThemeHelper
{
    private static ElementTheme _currentTheme = ElementTheme.Default;

    public static ElementTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            ApplyThemeToAllWindows();
            ThemeChanged?.Invoke(null, _currentTheme);
        }
    }

    public static event EventHandler<ElementTheme>? ThemeChanged;

    public static void Initialize()
    {
        var savedTheme = SettingsHelper.GetSetting<string>("AppTheme");
        if (!string.IsNullOrEmpty(savedTheme) && Enum.TryParse<ElementTheme>(savedTheme, out var theme))
        {
            _currentTheme = theme;
        }
    }

    public static ElementTheme GetActualTheme()
    {
        if (_currentTheme == ElementTheme.Default)
        {
            return Application.Current.RequestedTheme == ApplicationTheme.Dark
                ? ElementTheme.Dark
                : ElementTheme.Light;
        }
        return _currentTheme;
    }

    private static void ApplyThemeToAllWindows()
    {
        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window.Content is FrameworkElement root)
            {
                root.RequestedTheme = _currentTheme;
            }
        }
    }
}
