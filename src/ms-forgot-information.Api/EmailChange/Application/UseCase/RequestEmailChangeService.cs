using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.EmailChange.Application.Dto;
using ms_forgot_information.Api.EmailChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.EmailChange.Application.UseCase;

/// <summary>
/// Behaves identically whether the email exists or not (no account enumeration). The code only ever goes
/// to the address IAM returns for that profile, never to an address supplied by the caller.
/// </summary>
public class RequestEmailChangeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ILogger<RequestEmailChangeService> logger) : IRequestEmailChangeUseCase
{
    public async Task ExecuteAsync(RequestEmailChangeDto dto, string requestIp, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct);

        if (profile is null)
        {
            logger.LogInformation("Email change requested for an unknown email {Email}: no code sent", EmailLogMask.Mask(email));
            return;
        }

        try
        {
            await verificationCodeService.IssueAsync(profile.ProfileId, Purpose.EmailChange, profile.Email, requestIp, ct);
        }
        catch (TooManyRequestsException)
        {
            logger.LogInformation("Email change rate-limited for profile {ProfileId}", profile.ProfileId);
        }
        catch (NotificationDeliveryException)
        {
            logger.LogWarning("Email change code could not be delivered for profile {ProfileId}", profile.ProfileId);
        }
    }
}