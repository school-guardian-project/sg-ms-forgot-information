using ms_forgot_information.Api.EmailChange.Application.Dto;
using ms_forgot_information.Api.EmailChange.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.EmailChange.Application.UseCase;

public class VerifyEmailChangeCodeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService) : IVerifyEmailChangeCodeUseCase
{
    public Task<VerifyEmailChangeCodeResponseDto> VerifyCurrentAsync(VerifyEmailChangeCodeDto dto, CancellationToken ct) =>
        VerifyAsync(dto, Purpose.EmailChange, ct);

    public Task<VerifyEmailChangeCodeResponseDto> VerifyNewAsync(VerifyEmailChangeCodeDto dto, CancellationToken ct) =>
        VerifyAsync(dto, Purpose.EmailChangeConfirm, ct);

    private async Task<VerifyEmailChangeCodeResponseDto> VerifyAsync(VerifyEmailChangeCodeDto dto, Purpose purpose, CancellationToken ct)
    {
        var email = InputValidators.NormalizeEmail(dto.Email);
        var profile = await identityDirectory.FindProfileByEmailAsync(email, ct)
            ?? throw new InvalidCodeException();

        var ticket = await verificationCodeService.VerifyAsync(profile.ProfileId, purpose, dto.Code, ct);
        return new VerifyEmailChangeCodeResponseDto(ticket.ResetToken);
    }
}