namespace Scheduler.Api.Services;

public record SmsResult(bool Success, string? ProviderMessageId, string? Error);

public interface ISmsSender
{
    Task<SmsResult> SendAsync(string toPhoneNumber, string body, CancellationToken ct = default);
}
