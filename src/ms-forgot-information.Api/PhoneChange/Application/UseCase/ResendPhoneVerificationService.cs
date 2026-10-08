using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.PhoneChange.Application.Dto;
using ms_forgot_information.Api.PhoneChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.PhoneChange.Application.UseCase;

public class ResendPhoneVerificationService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ISmsVerificationService smsVerification,
    ILogger<ResendPhoneVerificationService> logger) : IResendPhoneVerificationUseCase
{
    public async Task ExecuteAsync(ResendPhoneVerificationDto dto, string requestIp, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var newPhone = InputValidators.NormalizePhone(dto.NewPhone);

        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidResetTokenException();

        var previous = await verificationCodeService.GetResendableChallengeAsync(profile.ProfileId, Purpose.PhoneChangeConfirm, ct);
        if (!string.Equals(previous.Target, newPhone, StringComparison.Ordinal))
        {
            throw new InvalidCodeException();
        }

        var challenge = await verificationCodeService.OpenExternalChallengeAsync(
            profile.ProfileId, Purpose.PhoneChangeConfirm, newPhone, requestIp, ct);

        try
        {
            await smsVerification.StartAsync(newPhone, ct);
        }
        catch
        {
            await verificationCodeService.AbandonAsync(challenge, ct);
            throw;
        }

        logger.LogInformation("Phone verification resent for profile {ProfileId}", profile.ProfileId);
    }
}
