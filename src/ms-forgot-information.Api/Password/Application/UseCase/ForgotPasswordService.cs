using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;

namespace ms_forgot_information.Api.Password.Application.UseCase;

/// <summary>
/// Reports why no code was sent (unknown account, rate limit, delivery failure) so the client can tell the user.
/// </summary>
public class ForgotPasswordService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ILogger<ForgotPasswordService> logger) : IForgotPasswordUseCase
{
    public async Task ExecuteAsync(ForgotPasswordRequestDto dto, string requestIp, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct);

        if (profile is null)
        {
            logger.LogInformation("Password reset requested for an unknown email {Email}: IAM returned no active profile, no code sent", EmailLogMask.Mask(email));
            throw new AccountNotFoundException();
        }

        logger.LogInformation("IAM resolved {Requested} to recipient {Recipient}", EmailLogMask.Mask(email), EmailLogMask.Mask(profile.Email));

        try
        {
            await verificationCodeService.IssueAsync(profile.ProfileId, Purpose.PasswordReset, profile.Email, requestIp, ct, dto.IsPasswordChange);
        }
        catch (TooManyRequestsException)
        {
            logger.LogInformation("Password reset rate-limited for profile {ProfileId}", profile.ProfileId);
            throw;
        }
        catch (NotificationDeliveryException)
        {
            logger.LogWarning("Password reset code could not be delivered for profile {ProfileId}", profile.ProfileId);
            throw;
        }
    }
}
