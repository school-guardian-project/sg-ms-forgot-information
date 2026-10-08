using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;

namespace ms_forgot_information.Api.Password.Application.UseCase;

public class VerifyPasswordResetCodeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService) : IVerifyPasswordResetCodeUseCase
{
    public async Task<VerifyPasswordResetCodeResponseDto> ExecuteAsync(VerifyPasswordResetCodeRequestDto dto, CancellationToken ct)
    {
        // Same generic error as an invalid code — this endpoint must not leak whether the email exists either.
        var email = InputValidators.NormalizeEmail(dto.Email);
        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidCodeException();

        var ticket = await verificationCodeService.VerifyAsync(profile.ProfileId, Purpose.PasswordReset, dto.Code, ct);

        return new VerifyPasswordResetCodeResponseDto(ticket.ResetToken);
    }
}
