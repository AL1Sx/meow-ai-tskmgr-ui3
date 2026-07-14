using Microsoft.UI.Xaml.Controls;
using meow_ai_tskmgr_ui3.ViewModels;

namespace meow_ai_tskmgr_ui3.Pages;

public sealed partial class HardwareInfoPage : Page
{
    public SystemInfoViewModel ViewModel { get; }

    public HardwareInfoPage()
    {
        this.InitializeComponent();
        ViewModel = App.SystemInfoViewModel!;
    }
}
