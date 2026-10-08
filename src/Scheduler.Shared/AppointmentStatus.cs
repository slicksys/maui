namespace Scheduler.Shared;

public enum AppointmentStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    NoShow = 3
}

public enum SmsStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
