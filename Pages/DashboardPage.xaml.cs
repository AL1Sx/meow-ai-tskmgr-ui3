using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using meow_ai_tskmgr_ui3.ViewModels;

namespace meow_ai_tskmgr_ui3.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }
    private bool _hasInitialized = false;

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = App.DashboardViewModel!;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!_hasInitialized)
        {
            ViewModel.StartMonitoring();  // 首次进入，触发分析
            _hasInitialized = true;
        }
        else
        {
            ViewModel.ResumeMonitoring();  // 返回页面，只恢复定时器
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopMonitoring();
    }
}
