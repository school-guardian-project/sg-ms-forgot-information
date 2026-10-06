using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.PhoneChange.Application.Dto;
using ms_forgot_information.Api.PhoneChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.PhoneChange.Application.UseCase;

/// <summary>
/// Sends the identity SMS (Twilio Verify) to the CURRENT phone. Completes silently when the email is unknown or the
/// phone does not match the stored one, so the endpoint cannot be used to discover accounts or phones.
/// </summary>
public class RequestPhoneChangeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ISmsVerificationService smsVerification,
    ILogger<RequestPhoneChangeService> logger) : IRequestPhoneChangeUseCase
{
    public async Task ExecuteAsync(RequestPhoneChangeDto dto, string requestIp, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var currentPhone = InputValidators.NormalizePhone(dto.CurrentPhone);
        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct);

        if (profile is null || !await identityDirectory.PhoneMatchesAsync(profile.ProfileId, currentPhone, ct))
        {
            logger.LogInformation("Phone change identity requested for an unknown email or non-matching phone: no SMS sent");
            return;
        }

        try
        {
            var challenge = await verificationCodeService.OpenExternalChallengeAsync(
                profile.ProfileId, Purpose.PhoneChange, currentPhone, requestIp, ct);

            try
            {
                await smsVerification.StartAsync(currentPhone, ct);
            }
            catch
            {
                await verificationCodeService.AbandonAsync(challenge, ct);
                throw;
            }

            logger.LogInformation("Phone change identity SMS started for profile {ProfileId}", profile.ProfileId);
        }
        catch (TooManyRequestsException)
        {
            logger.LogInformation("Phone change rate-limited for profile {ProfileId}", profile.ProfileId);
        }
        catch (VerificationException ex)
        {
            logger.LogWarning("Phone change identity SMS could not be sent for profile {ProfileId}: {Reason}", profile.ProfileId, ex.GetType().Name);
        }
    }
}