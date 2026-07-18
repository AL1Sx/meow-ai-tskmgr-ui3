using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace meow_ai_tskmgr_ui3;

public class PercentageToWidthConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 120;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float percentage)
        {
            return percentage / 100.0 * MaxWidth;
        }
        if (value is double doubleValue)
        {
            return doubleValue / 100.0 * MaxWidth;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class FloatToStringConverter : IValueConverter
{
    public string Format { get; set; } = "F1";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float floatValue)
        {
            return floatValue.ToString(Format) + "%";
        }
        if (value is double doubleValue)
        {
            return doubleValue.ToString(Format) + "%";
        }
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class UInt64ToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ulong ulongValue)
        {
            return ulongValue.ToString("N0") + " MB";
        }
        return "0 MB";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrEmpty(value as string)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

public class BoolNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}
