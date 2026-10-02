using ms_forgot_information.Api.Email.Application.Dto;
using ms_forgot_information.Api.Email.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;

namespace ms_forgot_information.Api.Email.Application.UseCase;

/// <summary>The new email is never considered verified before the code sent TO IT is confirmed.</summary>
public class RequestEmailChangeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService) : IRequestEmailChangeUseCase
{
    public async Task ExecuteAsync(Guid profileId, RequestEmailChangeDto dto, string requestIp, CancellationToken ct)
    {
        InputValidators.ValidateEmail(dto.NewEmail);

        if (await identityDirectory.EmailInUseAsync(dto.NewEmail, ct))
        {
            throw new DuplicateContactException("Ese correo ya está en uso por otra cuenta.");
        }

        await verificationCodeService.IssueAsync(profileId, Purpose.ChangeEmail, dto.NewEmail, requestIp, ct);
    }
}
