using System;
using Microsoft.UI.Xaml;
using meow_ai_tskmgr_ui3.Services;
using meow_ai_tskmgr_ui3.ViewModels;

namespace meow_ai_tskmgr_ui3;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public static ConfigService? ConfigService { get; private set; }
    public static AIService? AIService { get; private set; }
    public static SystemMonitorService? MonitorService { get; private set; }

    public static DashboardViewModel? DashboardViewModel { get; private set; }
    public static SettingsViewModel? SettingsViewModel { get; private set; }
    public static SystemInfoViewModel? SystemInfoViewModel { get; private set; }

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Message}");
            e.Handled = true;
        };
        InitializeServices();
    }

    private void InitializeServices()
    {
        try
        {
            ConfigService = new ConfigService();
            AIService = new AIService(ConfigService);
            MonitorService = new SystemMonitorService();

            DashboardViewModel = new DashboardViewModel(MonitorService, AIService, ConfigService);
            SettingsViewModel = new SettingsViewModel(ConfigService);
            SystemInfoViewModel = new SystemInfoViewModel(MonitorService, AIService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Service initialization error: {ex.Message}");
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Initialize converters after XAML is loaded
        InitializeConverters();

        MainWindow = new MainWindow();
        MainWindow.Closed += (sender, args) =>
        {
            MonitorService?.Dispose();
            (AIService as IDisposable)?.Dispose();
        };
        MainWindow.Activate();
    }

    private void InitializeConverters()
    {
        try
        {
            // Add converters to application resources
            if (!Resources.ContainsKey("FloatToStringConverter"))
                Resources["FloatToStringConverter"] = new FloatToStringConverter();
            if (!Resources.ContainsKey("UInt64ToStringConverter"))
                Resources["UInt64ToStringConverter"] = new UInt64ToStringConverter();
            if (!Resources.ContainsKey("StringToVisibilityConverter"))
                Resources["StringToVisibilityConverter"] = new StringToVisibilityConverter();
            if (!Resources.ContainsKey("PercentageToWidthConverter"))
                Resources["PercentageToWidthConverter"] = new PercentageToWidthConverter { MaxWidth = 120 };
            if (!Resources.ContainsKey("BoolNegationConverter"))
                Resources["BoolNegationConverter"] = new BoolNegationConverter();
            if (!Resources.ContainsKey("BooleanToVisibilityConverter"))
                Resources["BooleanToVisibilityConverter"] = new BooleanToVisibilityConverter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Converter initialization error: {ex.Message}");
        }
    }
}
