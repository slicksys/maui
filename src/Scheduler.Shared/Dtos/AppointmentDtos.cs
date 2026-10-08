namespace Scheduler.Shared.Dtos;

public record AppointmentDto(
    int Id,
    int ClientId,
    string ClientName,
    string ClientPhone,
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    AppointmentStatus Status,
    string? Color,
    SmsMessageDto? LastSms,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public DateTime StartLocal => StartUtc.ToLocalTime();
    public DateTime EndLocal => EndUtc.ToLocalTime();
}

public record AppointmentUpsertDto(
    int ClientId,
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    AppointmentStatus Status,
    string? Color);

public record SmsMessageDto(
    int Id,
    string ToPhoneNumber,
    string Body,
    SmsStatus Status,
    string? ProviderMessageId,
    string? Error,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);
