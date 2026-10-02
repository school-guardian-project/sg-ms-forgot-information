using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;

namespace ms_forgot_information.Api.Password.Application.UseCase;

/// <summary>
/// DEC-003 (sg-docs/.../12-forgot-information/decisions.md): requires EXACTLY ONE of the
/// current password or a verified OTP — never both, never neither.
/// </summary>
public class ChangePasswordService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    IDomainEventPublisher eventPublisher) : IChangePasswordUseCase
{
    public async Task ExecuteAsync(Guid profileId, ChangePasswordRequestDto dto, CancellationToken ct)
    {
        InputValidators.ValidateNewPassword(dto.NewPassword, dto.ConfirmPassword);

        var hasCurrentPassword = !string.IsNullOrWhiteSpace(dto.CurrentPassword);
        var hasCode = !string.IsNullOrWhiteSpace(dto.Code);

        if (hasCurrentPassword == hasCode)
        {
            throw new InvalidCredentialsException();
        }

        Guid? verifiedRequestId = null;

        if (hasCurrentPassword)
        {
            var valid = await identityDirectory.ValidateCurrentPasswordAsync(profileId, dto.CurrentPassword!, ct);
            if (!valid)
            {
                throw new InvalidCredentialsException();
            }
        }
        else
        {
            var ticket = await verificationCodeService.VerifyAsync(profileId, Purpose.ChangePassword, dto.Code!, ct);
            verifiedRequestId = ticket.RequestId;
        }

        try
        {
            await identityDirectory.UpdatePasswordAsync(profileId, dto.NewPassword, ct);
        }
        catch (Exception ex) when (ex is not VerificationException)
        {
            throw new UpstreamUpdateException(string.Empty, ex);
        }

        if (verifiedRequestId is not null)
        {
            await verificationCodeService.CompleteAsync(verifiedRequestId.Value, ct);
        }

        await eventPublisher.PublishAsync(
            "PasswordChanged",
            "forgot-information.password.changed",
            new { profileId, occurredAt = DateTime.UtcNow },
            ct);
    }
}
