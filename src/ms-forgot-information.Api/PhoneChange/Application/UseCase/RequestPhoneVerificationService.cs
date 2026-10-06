using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.PhoneChange.Application.Dto;
using ms_forgot_information.Api.PhoneChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.PhoneChange.Application.UseCase;

public class RequestPhoneVerificationService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ISmsVerificationService smsVerification,
    ILogger<RequestPhoneVerificationService> logger) : IRequestPhoneVerificationUseCase
{
    public async Task ExecuteAsync(RequestPhoneVerificationDto dto, string requestIp, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        // Validated before anything else so an invalid number never reaches Twilio or burns the identity token.
        var newPhone = InputValidators.NormalizePhone(dto.NewPhone);

        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidResetTokenException();

        var ticket = await verificationCodeService.ConsumeResetTokenAsync(profile.ProfileId, Purpose.PhoneChange, dto.ResetToken, ct);

        var challenge = await verificationCodeService.OpenExternalChallengeAsync(
            profile.ProfileId, Purpose.PhoneChangeConfirm, newPhone, requestIp, ct);

        try
        {
            // The recipient is exactly the validated NewPhone: no stored, configured or previous number is involved.
            await smsVerification.StartAsync(newPhone, ct);
        }
        catch
        {
            await verificationCodeService.AbandonAsync(challenge, ct);
            throw;
        }

        // Identity token is single-use, but only burned once the SMS was actually requested (retryable on failure).
        await verificationCodeService.CompleteAsync(ticket.RequestId, ct);
        logger.LogInformation("Phone verification started for profile {ProfileId}", profile.ProfileId);
    }
}