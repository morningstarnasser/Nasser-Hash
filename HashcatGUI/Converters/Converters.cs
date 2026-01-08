using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HashcatGUI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            if (parameter?.ToString() == "Inverse")
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNullOrEmpty = value == null;

        // Also treat 0 as "empty" for count values
        if (value is int intValue)
            isNullOrEmpty = intValue == 0;
        else if (value is long longValue)
            isNullOrEmpty = longValue == 0;

        if (parameter?.ToString() == "Inverse")
            return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
        return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isNotEmpty = !string.IsNullOrEmpty(value?.ToString());
        if (parameter?.ToString() == "Inverse")
            return isNotEmpty ? Visibility.Collapsed : Visibility.Visible;
        return isNotEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ProgressToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            if (progress >= 100)
                return new SolidColorBrush(Color.FromRgb(0, 217, 165)); // Success
            if (progress >= 75)
                return new SolidColorBrush(Color.FromRgb(84, 160, 255)); // Info
            if (progress >= 50)
                return new SolidColorBrush(Color.FromRgb(255, 179, 71)); // Warning
            return new SolidColorBrush(Color.FromRgb(233, 69, 96)); // Accent
        }
        return new SolidColorBrush(Color.FromRgb(233, 69, 96));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F2} GB";
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status.ToLowerInvariant() switch
            {
                "running" => new SolidColorBrush(Color.FromRgb(0, 217, 165)),
                "completed" or "cracked" => new SolidColorBrush(Color.FromRgb(0, 217, 165)),
                "paused" => new SolidColorBrush(Color.FromRgb(255, 179, 71)),
                "error" or "aborted" => new SolidColorBrush(Color.FromRgb(255, 71, 87)),
                "exhausted" => new SolidColorBrush(Color.FromRgb(84, 160, 255)),
                _ => new SolidColorBrush(Color.FromRgb(176, 176, 176))
            };
        }
        return new SolidColorBrush(Color.FromRgb(176, 176, 176));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
            return parameter;
        return Binding.DoNothing;
    }
}

public class MultiValueBooleanAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.All(v => v is bool b && b);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PercentToWidthConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 400;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            return Math.Max(0, Math.Min(MaxWidth, percent / 100 * MaxWidth));
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class QueueStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HashcatGUI.Models.QueueStatus status)
        {
            return status switch
            {
                HashcatGUI.Models.QueueStatus.Pending => "Clock",
                HashcatGUI.Models.QueueStatus.Analyzing => "Magnify",
                HashcatGUI.Models.QueueStatus.Ready => "CheckCircle",
                HashcatGUI.Models.QueueStatus.Running => "Play",
                HashcatGUI.Models.QueueStatus.Completed => "CheckAll",
                HashcatGUI.Models.QueueStatus.Failed => "AlertCircle",
                HashcatGUI.Models.QueueStatus.Skipped => "SkipNext",
                _ => "HelpCircle"
            };
        }
        return "HelpCircle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class QueueStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HashcatGUI.Models.QueueStatus status)
        {
            return status switch
            {
                HashcatGUI.Models.QueueStatus.Pending => new SolidColorBrush(Color.FromRgb(176, 176, 176)),
                HashcatGUI.Models.QueueStatus.Analyzing => new SolidColorBrush(Color.FromRgb(84, 160, 255)),
                HashcatGUI.Models.QueueStatus.Ready => new SolidColorBrush(Color.FromRgb(0, 217, 165)),
                HashcatGUI.Models.QueueStatus.Running => new SolidColorBrush(Color.FromRgb(233, 69, 96)),
                HashcatGUI.Models.QueueStatus.Completed => new SolidColorBrush(Color.FromRgb(0, 217, 165)),
                HashcatGUI.Models.QueueStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 71, 87)),
                HashcatGUI.Models.QueueStatus.Skipped => new SolidColorBrush(Color.FromRgb(255, 179, 71)),
                _ => new SolidColorBrush(Color.FromRgb(176, 176, 176))
            };
        }
        return new SolidColorBrush(Color.FromRgb(176, 176, 176));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

