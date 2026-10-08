using Scheduler.Api.Models;
using Scheduler.Shared.Dtos;

namespace Scheduler.Api.Data;

public static class Mapping
{
    public static ClientDto ToDto(this Client c) =>
        new(c.Id, c.FirstName, c.LastName, c.PhoneNumber, c.Email, c.Notes, c.CreatedAtUtc);

    public static SmsMessageDto ToDto(this SmsMessage s) =>
        new(s.Id, s.ToPhoneNumber, s.Body, s.Status, s.ProviderMessageId, s.Error, s.CreatedAtUtc, s.SentAtUtc);

    public static AppointmentDto ToDto(this Appointment a) =>
        new(a.Id,
            a.ClientId,
            a.Client?.FullName ?? string.Empty,
            a.Client?.PhoneNumber ?? string.Empty,
            a.Title,
            a.Description,
            DateTime.SpecifyKind(a.StartUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(a.EndUtc, DateTimeKind.Utc),
            a.Location,
            a.Status,
            a.Color,
            a.SmsMessages?.OrderByDescending(s => s.CreatedAtUtc).FirstOrDefault()?.ToDto(),
            a.CreatedAtUtc,
            a.UpdatedAtUtc);
}
