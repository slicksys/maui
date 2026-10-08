namespace Scheduler.Api.Services;

public class TwilioOptions
{
    public const string SectionName = "Twilio";
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    /// <summary>Sending number in E.164 format, or a Messaging Service SID (MG...).</summary>
    public string? FromNumber { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountSid) &&
        !string.IsNullOrWhiteSpace(AuthToken) &&
        !string.IsNullOrWhiteSpace(FromNumber);
}
