using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Scheduler.Api.Services;

public class TwilioSmsSender : ISmsSender
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(IOptions<TwilioOptions> options, ILogger<TwilioSmsSender> logger)
    {
        _options = options.Value;
        _logger = logger;
        TwilioClient.Init(_options.AccountSid, _options.AuthToken);
    }

    public async Task<SmsResult> SendAsync(string toPhoneNumber, string body, CancellationToken ct = default)
    {
        try
        {
            var createOptions = new CreateMessageOptions(new PhoneNumber(toPhoneNumber)) { Body = body };
            var from = _options.FromNumber ?? string.Empty;
            if (from.StartsWith("MG", StringComparison.OrdinalIgnoreCase))
                createOptions.MessagingServiceSid = from;
            else
                createOptions.From = new PhoneNumber(from);

            var message = await MessageResource.CreateAsync(createOptions);
            if (message.ErrorCode is not null)
            {
                _logger.LogWarning("Twilio rejected SMS to {To}: {Code} {Message}", toPhoneNumber, message.ErrorCode, message.ErrorMessage);
                return new SmsResult(false, message.Sid, $"{message.ErrorCode}: {message.ErrorMessage}");
            }

            _logger.LogInformation("SMS {Sid} queued to {To}", message.Sid, toPhoneNumber);
            return new SmsResult(true, message.Sid, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}", toPhoneNumber);
            return new SmsResult(false, null, ex.Message);
        }
    }
}
