using ms_forgot_information.Api.PhoneChange.Application.Dto;
using ms_forgot_information.Api.PhoneChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.PhoneChange.Application.UseCase;

public class VerifyPhoneChangeIdentityService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ISmsVerificationService smsVerification) : IVerifyPhoneChangeIdentityUseCase
{
    public async Task<VerifyPhoneChangeIdentityResponseDto> ExecuteAsync(VerifyPhoneChangeIdentityDto dto, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var currentPhone = InputValidators.NormalizePhone(dto.CurrentPhone);

        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidCodeException();

        var challenge = await verificationCodeService.GetPendingExternalChallengeAsync(profile.ProfileId, Purpose.PhoneChange, ct);

        // The code must be checked against the number the SMS was sent to.
        if (!string.Equals(challenge.Target, currentPhone, StringComparison.Ordinal))
        {
            throw new InvalidCodeException();
        }

        var result = await smsVerification.CheckAsync(challenge.Target, dto.Code, ct);
        if (result != SmsCheckResult.Approved)
        {
            await verificationCodeService.RegisterExternalFailureAsync(challenge, ct);
            throw new InvalidCodeException();
        }

        var ticket = await verificationCodeService.ApproveExternalAsync(challenge, ct);
        return new VerifyPhoneChangeIdentityResponseDto(ticket.ResetToken);
    }
}