using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using meow_ai_tskmgr_ui3.Pages;

namespace meow_ai_tskmgr_ui3;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // 设置窗口大小为 630x980
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(630, 980));

                // 居中显示
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                    windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var centerX = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                    var centerY = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                    appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Window setup error: {ex.Message}");
        }

        NavigationView.SelectedItem = DashboardItem;
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            Type? pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "HardwareInfo" => typeof(HardwareInfoPage),
                "ProcessList" => typeof(ProcessListPage),
                "Settings" => typeof(SettingsPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
