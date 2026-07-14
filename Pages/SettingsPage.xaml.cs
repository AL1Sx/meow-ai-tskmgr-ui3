using Microsoft.UI.Xaml.Controls;
using meow_ai_tskmgr_ui3.ViewModels;

namespace meow_ai_tskmgr_ui3.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.SettingsViewModel!;
    }
}
