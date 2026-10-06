namespace ms_forgot_information.Api.Shared.Domain.Port.Out;

public enum SmsCheckResult
{
    Approved,
    /// <summary>The code did not match (Twilio status other than "approved").</summary>
    Rejected
}

/// <summary>
/// SMS verification delegated to an external provider (Twilio Verify). The provider generates, sends,
/// expires and validates the OTP; this application never sees or stores it.
/// </summary>
public interface ISmsVerificationService
{
    /// <param name="e164Phone">Destination in E.164, passed to the provider exactly as received.</param>
    Task StartAsync(string e164Phone, CancellationToken ct);

    Task<SmsCheckResult> CheckAsync(string e164Phone, string code, CancellationToken ct);
}