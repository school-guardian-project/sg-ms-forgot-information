using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.EmailChange.Application.Dto;
using ms_forgot_information.Api.EmailChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.EmailChange.Application.UseCase;

public class SubmitNewEmailService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    ILogger<SubmitNewEmailService> logger) : ISubmitNewEmailUseCase
{
    public async Task ExecuteAsync(SubmitNewEmailDto dto, string requestIp, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var newEmail = InputValidators.NormalizeEmail(dto.NewEmail);

        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidResetTokenException();

        // Proves the caller controls the current mailbox. The token is only burned after the new code is sent.
        var ticket = await verificationCodeService.ConsumeResetTokenAsync(profile.ProfileId, Purpose.EmailChange, dto.ResetToken, ct);

        if (string.Equals(newEmail, profile.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidContactFormatException("El nuevo correo debe ser distinto al actual.");
        }

        if (await identityDirectory.FindProfileByEmailAsync(newEmail, ct) is not null)
        {
            throw new EmailAlreadyInUseException();
        }

        logger.LogInformation("Email change for profile {ProfileId}: sending code to new address {NewEmail}", profile.ProfileId, EmailLogMask.Mask(newEmail));
        await verificationCodeService.IssueAsync(profile.ProfileId, Purpose.EmailChangeConfirm, newEmail, requestIp, ct);
        await verificationCodeService.CompleteAsync(ticket.RequestId, ct);
    }
}