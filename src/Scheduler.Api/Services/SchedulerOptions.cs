namespace Scheduler.Api.Services;

public class SchedulerOptions
{
    public const string SectionName = "Scheduler";
    public string BusinessName { get; set; } = "Our Office";
    /// <summary>IANA or Windows time zone id used to format times in SMS messages.</summary>
    public string TimeZone { get; set; } = "UTC";

    public TimeZoneInfo ResolveTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(TimeZone); }
        catch { return TimeZoneInfo.Utc; }
    }
}
