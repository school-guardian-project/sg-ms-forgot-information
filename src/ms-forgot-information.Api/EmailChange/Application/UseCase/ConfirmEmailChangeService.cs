using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.EmailChange.Application.Dto;
using ms_forgot_information.Api.EmailChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.EmailChange.Application.UseCase;

public class ConfirmEmailChangeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ILogger<ConfirmEmailChangeService> logger) : IConfirmEmailChangeUseCase
{
    public async Task ExecuteAsync(ConfirmEmailChangeDto dto, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidResetTokenException();

        // The confirm request exists only if the current mailbox was proven first, and its Target is the new address.
        var ticket = await verificationCodeService.ConsumeResetTokenAsync(profile.ProfileId, Purpose.EmailChangeConfirm, dto.ResetToken, ct);

        try
        {
            await identityDirectory.UpdateEmailAsync(ticket.ProfileId, ticket.Target, ct);
        }
        catch (Exception ex) when (ex is not VerificationException)
        {
            throw new UpstreamUpdateException(string.Empty, ex);
        }

        await verificationCodeService.CompleteAsync(ticket.RequestId, ct);
        logger.LogInformation("Email changed for profile {ProfileId}: {Old} -> {New}",
            profile.ProfileId, EmailLogMask.Mask(profile.Email), EmailLogMask.Mask(ticket.Target));
    }
}