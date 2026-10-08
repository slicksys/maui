using Scheduler.Shared;

namespace Scheduler.Api.Models;

public class Appointment
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Location { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    /// <summary>Hex colour tag chosen in the UI, e.g. #6366F1.</summary>
    public string? Color { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<SmsMessage> SmsMessages { get; set; } = [];
}
