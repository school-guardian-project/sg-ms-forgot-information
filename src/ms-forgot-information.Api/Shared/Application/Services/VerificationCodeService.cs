using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ms_forgot_information.Api.Shared.Application.Services;

public sealed record VerificationTicket(Guid RequestId, Guid ProfileId, Purpose Purpose, string Target, string ResetToken);

/// <summary>
/// Implements the password-recovery code lifecycle (issue -> verify -> consume).
/// </summary>
public class VerificationCodeService(
    IVerificationRequestRepository repository,
    IEmailSender emailSender,
    ISecretHasher secretHasher,
    IOptions<OtpOptions> options,
    ILogger<VerificationCodeService> logger,
    TimeProvider timeProvider)
{
    private readonly OtpOptions _options = options.Value;

    public async Task IssueAsync(Guid profileId, Purpose purpose, string target, string requestIp, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.AddMinutes(-_options.RequestWindowMinutes);

        var recentCount = await repository.CountRecentAsync(profileId, purpose, windowStart, ct);
        if (recentCount >= _options.MaxRequestsPerWindow)
        {
            throw new TooManyRequestsException();
        }

        var code = OtpCodeGenerator.GenerateNumericCode(OtpOptions.ResetCodeLength);
        var codeHash = secretHasher.Hash(code);

        var request = VerificationRequest.Issue(
            profileId,
            purpose,
            target,
            codeHash,
            TimeSpan.FromMinutes(_options.ExpirationMinutes),
            _options.MaxAttempts,
            requestIp,
            now);

        await repository.AddAsync(request, ct);

        try
        {
            await DeliverAsync(target, code, purpose, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The code never reached the user: burn it so it can't be verified later. The row still
            // counts toward the rate limit, which also protects the mail provider from being hammered.
            request.MarkConsumed(now);
            await repository.UpdateAsync(request, ct);

            logger.LogError(ex, "Verification code delivery failed for profile {ProfileId}, purpose {Purpose}", profileId, purpose);
            throw ex as NotificationDeliveryException ?? new NotificationDeliveryException(ex);
        }

        logger.LogInformation("Verification code issued for profile {ProfileId}, purpose {Purpose}", profileId, purpose);
    }

    /// <summary>
    /// Opens a challenge whose OTP is owned by an external provider (Twilio Verify). Only the ledger row
    /// (target, expiry, attempts, rate limit) lives here; the placeholder hash can never match a user input.
    /// </summary>
    public async Task<VerificationRequest> OpenExternalChallengeAsync(Guid profileId, Purpose purpose, string target, string requestIp, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var recentCount = await repository.CountRecentAsync(profileId, purpose, now.AddMinutes(-_options.RequestWindowMinutes), ct);
        if (recentCount >= _options.MaxRequestsPerWindow)
        {
            throw new TooManyRequestsException();
        }

        var request = VerificationRequest.Issue(
            profileId, purpose, target,
            secretHasher.Hash(OtpCodeGenerator.GenerateOpaqueToken()),
            TimeSpan.FromMinutes(_options.ExpirationMinutes),
            _options.MaxAttempts, requestIp, now);

        await repository.AddAsync(request, ct);
        return request;
    }

    /// <summary>Burns a challenge whose provider call failed, so the row cannot be used later (it still counts for rate limiting).</summary>
    public async Task AbandonAsync(VerificationRequest request, CancellationToken ct)
    {
        request.MarkConsumed(timeProvider.GetUtcNow().UtcDateTime);
        await repository.UpdateAsync(request, ct);
    }

    /// <summary>Returns the live external challenge or throws; the caller then asks the provider to check the code.</summary>
    public async Task<VerificationRequest> GetPendingExternalChallengeAsync(Guid profileId, Purpose purpose, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var request = await repository.GetActiveAsync(profileId, purpose, ct)
            ?? throw new InvalidCodeException();

        if (request.Status == VerificationStatus.Locked)
        {
            throw new TooManyAttemptsException();
        }

        if (request.Status != VerificationStatus.Pending)
        {
            throw new InvalidCodeException();
        }

        if (request.IsExpired(now))
        {
            request.MarkExpired();
            await repository.UpdateAsync(request, ct);
            throw new CodeExpiredException();
        }

        return request;
    }

    /// <summary>The provider approved the code: marks the challenge verified and mints the opaque token for the next step.</summary>
    public async Task<VerificationTicket> ApproveExternalAsync(VerificationRequest request, CancellationToken ct)
    {
        var resetToken = OtpCodeGenerator.GenerateOpaqueToken();
        request.MarkVerified(secretHasher.Hash(resetToken), timeProvider.GetUtcNow().UtcDateTime);
        await repository.UpdateAsync(request, ct);
        return new VerificationTicket(request.Id, request.ProfileId, request.Purpose, request.Target, resetToken);
    }

    /// <summary>Counts a rejected code locally so a challenge cannot be brute-forced regardless of the provider limits.</summary>
    public async Task RegisterExternalFailureAsync(VerificationRequest request, CancellationToken ct)
    {
        request.RegisterFailedAttempt();
        await repository.UpdateAsync(request, ct);
        if (request.Status == VerificationStatus.Locked)
        {
            throw new TooManyAttemptsException();
        }
    }

    /// <summary>Compares the submitted code and, on success, mints a short-lived opaque token for the final step.</summary>
    public async Task<VerificationTicket> VerifyAsync(Guid profileId, Purpose purpose, string code, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var request = await repository.GetActiveAsync(profileId, purpose, ct)
            ?? throw new InvalidCodeException();

        if (request.Status == VerificationStatus.Locked)
        {
            throw new TooManyAttemptsException();
        }

        if (request.Status != VerificationStatus.Pending)
        {
            throw new InvalidCodeException();
        }

        if (request.IsExpired(now))
        {
            request.MarkExpired();
            await repository.UpdateAsync(request, ct);
            throw new CodeExpiredException();
        }

        if (!secretHasher.Verify(code, request.CodeHash))
        {
            request.RegisterFailedAttempt();
            await repository.UpdateAsync(request, ct);
            throw request.Status == VerificationStatus.Locked
                ? new TooManyAttemptsException()
                : new InvalidCodeException();
        }

        var resetToken = OtpCodeGenerator.GenerateOpaqueToken();
        request.MarkVerified(secretHasher.Hash(resetToken), now);
        await repository.UpdateAsync(request, ct);

        return new VerificationTicket(request.Id, request.ProfileId, request.Purpose, request.Target, resetToken);
    }

    /// <summary>Used by the dedicated "reset" step, which authenticates with the opaque token instead of the code.</summary>
    public async Task<VerificationTicket> ConsumeResetTokenAsync(Guid profileId, Purpose purpose, string resetToken, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var request = await repository.GetActiveAsync(profileId, purpose, ct)
            ?? throw new InvalidResetTokenException();

        var tokenExpiresAt = request.VerifiedAt?.AddMinutes(_options.ResetTokenExpirationMinutes);

        if (request.Status != VerificationStatus.Verified
            || request.ResetTokenHash is null
            || tokenExpiresAt is null
            || now >= tokenExpiresAt)
        {
            throw new InvalidResetTokenException();
        }

        if (!secretHasher.Verify(resetToken, request.ResetTokenHash))
        {
            throw new InvalidResetTokenException();
        }

        return new VerificationTicket(request.Id, request.ProfileId, request.Purpose, request.Target, resetToken);
    }

    /// <summary>
    /// Burns the request. Call this ONLY after the downstream update (password/email/phone)
    /// has actually succeeded — if it fails, leave the request as-is so the user can retry
    /// without being forced to request a brand-new code.
    /// </summary>
    public async Task CompleteAsync(Guid requestId, CancellationToken ct)
    {
        var request = await repository.GetByIdAsync(requestId, ct) ?? throw new InvalidCodeException();
        request.MarkConsumed(timeProvider.GetUtcNow().UtcDateTime);
        await repository.UpdateAsync(request, ct);
    }

    private async Task DeliverAsync(string target, string code, Purpose purpose, CancellationToken ct)
    {
        var (subject, intro) = purpose switch
        {
            Purpose.EmailChange => (
                "Código para cambiar tu correo — Guardian Escolar",
                "Tu código para confirmar el cambio de correo de tu cuenta de Guardian Escolar es"),
            Purpose.EmailChangeConfirm => (
                "Verifica tu nuevo correo — Guardian Escolar",
                "Tu código para verificar este correo como el nuevo correo de tu cuenta de Guardian Escolar es"),
            Purpose.PhoneChange => (
                "Código para cambiar tu teléfono — Guardian Escolar",
                "Tu código para confirmar el cambio de teléfono de tu cuenta de Guardian Escolar es"),
            _ => (
                "Código para recuperar tu contraseña — Guardian Escolar",
                "Tu código para recuperar la contraseña de Guardian Escolar es")
        };

        await emailSender.SendAsync(target, subject, BuildMessage(intro, code), ct);
    }

    private string BuildMessage(string intro, string code)
    {
        return $"{intro}: {code}.\n\n" +
               $"Vence en {_options.ExpirationMinutes} minutos y solo se puede usar una vez.\n" +
               "Si no solicitaste este cambio, ignora este correo.";
    }
}
