using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.EmailChange.Application.Dto;
using ms_forgot_information.Api.EmailChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.EmailChange.Application.UseCase;

public class ResendNewEmailService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ILogger<ResendNewEmailService> logger) : IResendNewEmailUseCase
{
    public async Task ExecuteAsync(ResendNewEmailDto dto, string requestIp, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);

        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidResetTokenException();

        var challenge = await verificationCodeService.GetResendableChallengeAsync(profile.ProfileId, Purpose.EmailChangeConfirm, ct);
        await verificationCodeService.IssueAsync(profile.ProfileId, Purpose.EmailChangeConfirm, challenge.Target, requestIp, ct);
        logger.LogInformation("Email change code resent for profile {ProfileId}", profile.ProfileId);
    }
}
