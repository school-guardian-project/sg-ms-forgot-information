using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.PhoneChange.Application.Dto;
using ms_forgot_information.Api.PhoneChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.PhoneChange.Application.UseCase;

public class CheckPhoneVerificationService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ISmsVerificationService smsVerification,
    ILogger<CheckPhoneVerificationService> logger) : ICheckPhoneVerificationUseCase
{
    public async Task ExecuteAsync(CheckPhoneVerificationDto dto, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var newPhone = InputValidators.NormalizePhone(dto.NewPhone);

        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidCodeException();

        var challenge = await verificationCodeService.GetPendingExternalChallengeAsync(profile.ProfileId, Purpose.PhoneChangeConfirm, ct);

        // The number being confirmed must be the one the SMS was sent to.
        if (!string.Equals(challenge.Target, newPhone, StringComparison.Ordinal))
        {
            throw new InvalidCodeException();
        }

        var result = await smsVerification.CheckAsync(challenge.Target, dto.Code, ct);
        if (result != SmsCheckResult.Approved)
        {
            await verificationCodeService.RegisterExternalFailureAsync(challenge, ct);
            throw new InvalidCodeException();
        }

        try
        {
            await identityDirectory.UpdatePhoneAsync(profile.ProfileId, challenge.Target, ct);
        }
        catch (Exception ex) when (ex is not VerificationException and not OperationCanceledException)
        {
            throw new UpstreamUpdateException(string.Empty, ex);
        }

        await verificationCodeService.CompleteAsync(challenge.Id, ct);
        logger.LogInformation("Phone updated for profile {ProfileId}", profile.ProfileId);
    }
}