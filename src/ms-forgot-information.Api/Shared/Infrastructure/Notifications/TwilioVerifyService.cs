using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Verify.V2.Service;

namespace ms_forgot_information.Api.Shared.Infrastructure.Notifications;

public sealed class TwilioVerifyService(
    IOptions<TwilioOptions> options,
    ILogger<TwilioVerifyService> logger) : ISmsVerificationService
{
    private readonly TwilioOptions _options = options.Value;

    public async Task StartAsync(string e164Phone, CancellationToken ct)
    {
        var client = CreateClient();
        try
        {
            await VerificationResource.CreateAsync(
                new CreateVerificationOptions(_options.VerifyServiceSid, e164Phone, "sms"),
                client);
        }
        catch (ApiException ex)
        {
            throw Translate(ex, "start");
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not VerificationException)
        {
            logger.LogError(ex, "Twilio Verify start failed unexpectedly");
            throw new NotificationDeliveryException(ex);
        }
    }

    public async Task<SmsCheckResult> CheckAsync(string e164Phone, string code, CancellationToken ct)
    {
        var client = CreateClient();
        try
        {
            var check = await VerificationCheckResource.CreateAsync(
                new CreateVerificationCheckOptions(_options.VerifyServiceSid) { To = e164Phone, Code = code },
                client);

            return string.Equals(check.Status, "approved", StringComparison.OrdinalIgnoreCase)
                ? SmsCheckResult.Approved
                : SmsCheckResult.Rejected;
        }
        catch (ApiException ex)
        {
            throw Translate(ex, "check");
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not VerificationException)
        {
            logger.LogError(ex, "Twilio Verify check failed unexpectedly");
            throw new NotificationDeliveryException(ex);
        }
    }

    private TwilioRestClient CreateClient()
    {
        if (!_options.IsConfigured)
        {
            logger.LogError("Twilio Verify is not configured (Twilio__AccountSid/ApiKey/ApiSecret/VerifyServiceSid)");
            throw new NotificationDeliveryException(new InvalidOperationException("Twilio Verify is not configured."));
        }

        // Instance client: no global TwilioClient state, credentials never logged.
        return new TwilioRestClient(_options.ApiKey, _options.ApiSecret, _options.AccountSid);
    }

    private Exception Translate(ApiException ex, string operation)
    {
        var mapped = TwilioErrorMapper.Map(ex.Code, ex.Status, operation);
        if (mapped is NotificationDeliveryException)
        {
            // Twilio code and HTTP status only; never the message body, which may echo the phone or account data.
            logger.LogError("Twilio Verify {Operation} failed: code {Code}, status {Status}", operation, ex.Code, ex.Status);
        }

        return mapped;
    }
}

/// <summary>Maps Twilio error codes to the application's exceptions so provider details never reach the client.</summary>
public static class TwilioErrorMapper
{
    public static Exception Map(int code, int httpStatus, string operation)
    {
        return code switch
        {
            // Invalid destination / not a mobile that can receive SMS.
            60200 or 60205 or 21211 or 21614 => new InvalidContactFormatException("El número de teléfono no es válido o no puede recibir SMS."),
            // Max send attempts reached.
            60203 => new TooManyRequestsException(),
            // Max check attempts reached.
            60202 => new TooManyAttemptsException(),
            // Verification not found: expired (10 min), already approved, or never started.
            20404 when operation == "check" => new CodeExpiredException(),
            _ when httpStatus == 429 => new TooManyRequestsException(),
            _ => new NotificationDeliveryException(new InvalidOperationException($"Twilio error {code} (HTTP {httpStatus})."))
        };
    }
}