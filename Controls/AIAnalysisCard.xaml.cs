using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace meow_ai_tskmgr_ui3.Controls;

public sealed partial class AIAnalysisCard : UserControl
{
    public static readonly DependencyProperty AnalysisContentProperty =
        DependencyProperty.Register(nameof(AnalysisContent), typeof(string), typeof(AIAnalysisCard), new PropertyMetadata(""));

    public static readonly DependencyProperty IsAnalyzingProperty =
        DependencyProperty.Register(nameof(IsAnalyzing), typeof(bool), typeof(AIAnalysisCard), new PropertyMetadata(false));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(AIAnalysisCard), new PropertyMetadata(""));

    public string AnalysisContent
    {
        get => (string)GetValue(AnalysisContentProperty);
        set => SetValue(AnalysisContentProperty, value);
    }

    public bool IsAnalyzing
    {
        get => (bool)GetValue(IsAnalyzingProperty);
        set => SetValue(IsAnalyzingProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public AIAnalysisCard()
    {
        this.InitializeComponent();
    }
}
