using Scheduler.Shared;

namespace Scheduler.Api.Models;

public class SmsMessage
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public required string ToPhoneNumber { get; set; }
    public required string Body { get; set; }
    public SmsStatus Status { get; set; } = SmsStatus.Pending;
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
}
