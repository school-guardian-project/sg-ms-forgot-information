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
/// Sends the identity SMS (Twilio Verify) to the CURRENT phone. Throws AccountNotFoundException or PhoneMismatchException so the client can say why no SMS was sent.
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

        if (profile is null)
        {
            logger.LogInformation("Phone change identity requested for an unknown email: no SMS sent");
            throw new AccountNotFoundException();
        }

        if (!await identityDirectory.PhoneMatchesAsync(profile.ProfileId, currentPhone, ct))
        {
            logger.LogInformation("Phone change identity requested with a non-matching phone for profile {ProfileId}: no SMS sent", profile.ProfileId);
            throw new PhoneMismatchException();
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
            throw;
        }
        catch (VerificationException ex)
        {
            logger.LogWarning("Phone change identity SMS could not be sent for profile {ProfileId}: {Reason}", profile.ProfileId, ex.GetType().Name);
            throw;
        }
    }
}