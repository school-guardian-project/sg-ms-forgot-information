using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.Shared.Infrastructure.Notifications;

/// <summary>
/// Logs the SMS instead of sending it. Used for local testing (Sms:Provider=Log) when there is
/// no real Twilio account — swap to <see cref="TwilioSmsSender"/> by setting Sms:Provider=Twilio.
/// </summary>
public class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string toPhoneE164, string message, CancellationToken ct)
    {
        logger.LogInformation("[dev-sms] to {Phone}: {Message}", toPhoneE164, message);
        return Task.CompletedTask;
    }
}
