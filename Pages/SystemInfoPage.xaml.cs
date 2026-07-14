using Microsoft.UI.Xaml.Controls;
using meow_ai_tskmgr_ui3.ViewModels;

namespace meow_ai_tskmgr_ui3.Pages;

public sealed partial class SystemInfoPage : Page
{
    public SystemInfoViewModel ViewModel { get; }

    public SystemInfoPage()
    {
        this.InitializeComponent();
        ViewModel = App.SystemInfoViewModel!;
    }
}
