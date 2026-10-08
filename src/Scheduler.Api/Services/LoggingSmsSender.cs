namespace Scheduler.Api.Services;

/// <summary>
/// Development fallback used when Twilio is not configured. Writes the message to the log
/// so the full scheduling flow can be exercised without an SMS account.
/// </summary>
public class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task<SmsResult> SendAsync(string toPhoneNumber, string body, CancellationToken ct = default)
    {
        logger.LogWarning("[SMS NOT SENT - Twilio not configured] To: {To}\n{Body}", toPhoneNumber, body);
        return Task.FromResult(new SmsResult(true, $"fake:{Guid.NewGuid():N}", null));
    }
}
