namespace Scheduler.Api.Models;

public class Client
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    /// <summary>E.164 formatted phone number, e.g. +15551234567.</summary>
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Appointment> Appointments { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();
}
