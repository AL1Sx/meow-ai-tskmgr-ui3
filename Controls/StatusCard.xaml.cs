using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace meow_ai_tskmgr_ui3.Controls;

public sealed partial class StatusCard : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(StatusCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatusCard), new PropertyMetadata(""));

    public static readonly DependencyProperty AdditionalInfoProperty =
        DependencyProperty.Register(nameof(AdditionalInfo), typeof(string), typeof(StatusCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(float), typeof(StatusCard), new PropertyMetadata(0f));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string AdditionalInfo
    {
        get => (string)GetValue(AdditionalInfoProperty);
        set => SetValue(AdditionalInfoProperty, value);
    }

    public float Progress
    {
        get => (float)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public StatusCard()
    {
        this.InitializeComponent();
    }
}
