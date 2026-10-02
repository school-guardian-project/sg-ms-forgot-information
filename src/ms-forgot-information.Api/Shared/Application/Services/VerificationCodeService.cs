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
/// The single place that implements the OTP state machine (issue -> verify -> consume) shared
/// by password recovery, password change, email change, and phone change. Delivery channel
/// (email vs SMS) is chosen automatically from the shape of <c>target</c>.
/// </summary>
public class VerificationCodeService(
    IVerificationRequestRepository repository,
    IEmailSender emailSender,
    ISmsSender smsSender,
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

        var code = OtpCodeGenerator.GenerateNumericCode(_options.CodeLength);
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

        await DeliverAsync(target, purpose, code, ct);

        logger.LogInformation("Verification code issued for profile {ProfileId}, purpose {Purpose}", profileId, purpose);
    }

    /// <summary>Compares the submitted code and, on success, mints a short-lived opaque token for the final step.</summary>
    public async Task<VerificationTicket> VerifyAsync(Guid profileId, Purpose purpose, string code, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var request = await repository.GetActiveAsync(profileId, purpose, ct)
            ?? throw new InvalidCodeException();

        if (request.Status == VerificationStatus.Consumed)
        {
            throw new InvalidCodeException();
        }

        if (request.Status == VerificationStatus.Locked)
        {
            throw new TooManyAttemptsException();
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

    private async Task DeliverAsync(string target, Purpose purpose, string code, CancellationToken ct)
    {
        var message = BuildMessage(purpose, code);

        if (target.Contains('@'))
        {
            await emailSender.SendAsync(target, "Código de verificación — Guardian Escolar", message, ct);
        }
        else
        {
            await smsSender.SendAsync(target, message, ct);
        }
    }

    private string BuildMessage(Purpose purpose, string code)
    {
        var action = purpose switch
        {
            Purpose.PasswordReset => "recuperar tu contraseña",
            Purpose.ChangePassword => "cambiar tu contraseña",
            Purpose.ChangeEmail => "confirmar tu nuevo correo",
            Purpose.ChangePhone => "confirmar tu nuevo teléfono",
            _ => "verificar tu identidad"
        };

        return $"Tu código para {action} en Guardian Escolar es: {code}. " +
               $"Expira en {_options.ExpirationMinutes} minutos. Si no solicitaste esto, ignora este mensaje.";
    }
}
