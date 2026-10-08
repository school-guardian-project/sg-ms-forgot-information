using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;

namespace ms_forgot_information.Api.Password.Application.UseCase;

public class ResetPasswordService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService) : IResetPasswordUseCase
{
    public async Task ExecuteAsync(ResetPasswordRequestDto dto, CancellationToken ct)
    {
        InputValidators.ValidateNewPassword(dto.NewPassword, dto.ConfirmPassword);

        var email = InputValidators.NormalizeEmail(dto.Email);
        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidResetTokenException();

        var ticket = await verificationCodeService.ConsumeResetTokenAsync(profile.ProfileId, Purpose.PasswordReset, dto.ResetToken, ct);

        try
        {
            await identityDirectory.UpdatePasswordAsync(ticket.ProfileId, dto.NewPassword, ct);
        }
        catch (Exception ex) when (ex is not VerificationException)
        {
            // The code/token stays usable (not yet consumed) so the user can retry without a new code.
            throw new UpstreamUpdateException(string.Empty, ex);
        }

        await verificationCodeService.CompleteAsync(ticket.RequestId, ct);
    }
}
