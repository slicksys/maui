using Scheduler.Api.Models;

namespace Scheduler.Api.Services;

public static class SmsTemplates
{
    public static string AppointmentBooked(Appointment a, string businessName, TimeZoneInfo tz)
    {
        var (start, end) = LocalRange(a, tz);
        var where = string.IsNullOrWhiteSpace(a.Location) ? "" : $" at {a.Location}";
        return $"Hi {a.Client.FirstName}, your appointment \"{a.Title}\" with {businessName} is confirmed for " +
               $"{start:ddd MMM d, yyyy} from {start:h:mm tt} to {end:h:mm tt}{where}. Reply STOP to opt out.";
    }

    public static string AppointmentUpdated(Appointment a, string businessName, TimeZoneInfo tz)
    {
        var (start, end) = LocalRange(a, tz);
        return $"Hi {a.Client.FirstName}, your appointment \"{a.Title}\" with {businessName} has been rescheduled to " +
               $"{start:ddd MMM d, yyyy} from {start:h:mm tt} to {end:h:mm tt}.";
    }

    public static string AppointmentCancelled(Appointment a, string businessName, TimeZoneInfo tz)
    {
        var (start, _) = LocalRange(a, tz);
        return $"Hi {a.Client.FirstName}, your appointment \"{a.Title}\" with {businessName} on " +
               $"{start:ddd MMM d} at {start:h:mm tt} has been cancelled.";
    }

    private static (DateTime Start, DateTime End) LocalRange(Appointment a, TimeZoneInfo tz)
    {
        var start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(a.StartUtc, DateTimeKind.Utc), tz);
        var end = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(a.EndUtc, DateTimeKind.Utc), tz);
        return (start, end);
    }
}
