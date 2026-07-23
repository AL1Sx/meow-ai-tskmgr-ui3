using Microsoft.UI.Xaml.Controls;
using meow_ai_tskmgr_ui3.ViewModels;

namespace meow_ai_tskmgr_ui3.Pages;

public sealed partial class ProcessListPage : Page
{
    public ProcessListViewModel ViewModel { get; }

    public ProcessListPage()
    {
        this.InitializeComponent();
        ViewModel = App.ProcessListViewModel!;
    }
}
