using System.Globalization;

namespace EVSwap.Mobile.Helpers;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRead)
            return isRead ? Colors.Gray : Colors.Black;
        return Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BatteryLevelToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double level)
        {
            if (level <= 10) return Colors.Red;
            if (level <= 25) return Colors.Orange;
            if (level <= 50) return Colors.Gold;
            return Colors.Green;
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status.ToLowerInvariant() switch
            {
                "active" or "available" or "completed" => Colors.Green,
                "pending" or "in_progress" => Colors.Orange,
                "fault" or "error" or "cancelled" => Colors.Red,
                "maintenance" => Colors.Blue,
                _ => Colors.Gray
            };
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}
