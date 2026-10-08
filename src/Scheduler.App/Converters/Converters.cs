using System.Globalization;
using Scheduler.App.Helpers;
using Scheduler.Shared;

namespace Scheduler.App.Converters;

/// <summary>Converts a hex string such as #6366F1 to a Color, falling back to the brand colour.</summary>
public class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && Color.TryParse(s, out var c)) return c;
        return Color.FromArgb("#6366F1");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color c ? c.ToArgbHex() : "#6366F1";
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        AppointmentStatus.Scheduled => Color.FromArgb("#6366F1"),
        AppointmentStatus.Completed => Color.FromArgb("#10B981"),
        AppointmentStatus.Cancelled => Color.FromArgb("#94A3B8"),
        AppointmentStatus.NoShow => Color.FromArgb("#F59E0B"),
        _ => Color.FromArgb("#6366F1")
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class StatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        AppointmentStatus.NoShow => "No-show",
        AppointmentStatus s => s.ToString(),
        _ => string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class SmsStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SmsStatus.Sent => Color.FromArgb("#10B981"),
        SmsStatus.Failed => Color.FromArgb("#FB7185"),
        SmsStatus.Pending => Color.FromArgb("#F59E0B"),
        _ => Color.FromArgb("#94A3B8")
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class SmsStatusToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SmsStatus.Sent => Icons.CheckCircle,
        SmsStatus.Failed => Icons.Error,
        SmsStatus.Pending => Icons.Hourglass,
        _ => Icons.Sms
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Turns "Ada Lovelace" into "AL" for avatar bubbles.</summary>
public class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return "?";
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
