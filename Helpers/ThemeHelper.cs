using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

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
            SettingsHelper.SetSetting("AppTheme", value.ToString());
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
            ApplyThemeToAllWindows();
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

    public static void UpdateTitleBarTheme(AppWindow appWindow)
    {
        if (appWindow == null) return;

        var titleBar = appWindow.TitleBar;
        var isDark = GetActualTheme() == ElementTheme.Dark;

        var buttonForeground = isDark
            ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
        var hoverBg = isDark
            ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x33, 0x00, 0x00, 0x00);
        var pressedBg = isDark
            ? Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x66, 0x00, 0x00, 0x00);
        var inactiveForeground = isDark
            ? Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x99, 0x00, 0x00, 0x00);

        titleBar.ButtonForegroundColor = buttonForeground;
        titleBar.ButtonBackgroundColor = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
        titleBar.ButtonHoverForegroundColor = buttonForeground;
        titleBar.ButtonHoverBackgroundColor = hoverBg;
        titleBar.ButtonPressedForegroundColor = buttonForeground;
        titleBar.ButtonPressedBackgroundColor = pressedBg;
        titleBar.InactiveForegroundColor = inactiveForeground;
        titleBar.InactiveBackgroundColor = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
    }

    private static void ApplyThemeToAllWindows()
    {
        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window.Content is FrameworkElement root)
            {
                root.RequestedTheme = _currentTheme;
            }

            try
            {
                UpdateTitleBarTheme(window.AppWindow);
            }
            catch { }
        }
    }
}
