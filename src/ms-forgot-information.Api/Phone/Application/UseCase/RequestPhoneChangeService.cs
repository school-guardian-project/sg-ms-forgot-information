using ms_forgot_information.Api.Phone.Application.Dto;
using ms_forgot_information.Api.Phone.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;

namespace ms_forgot_information.Api.Phone.Application.UseCase;

public class RequestPhoneChangeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService) : IRequestPhoneChangeUseCase
{
    public async Task ExecuteAsync(Guid profileId, RequestPhoneChangeDto dto, string requestIp, CancellationToken ct)
    {
        InputValidators.ValidatePhone(dto.NewPhone);

        if (await identityDirectory.PhoneInUseAsync(dto.NewPhone, ct))
        {
            throw new DuplicateContactException("Ese teléfono ya está en uso por otra cuenta.");
        }

        await verificationCodeService.IssueAsync(profileId, Purpose.ChangePhone, dto.NewPhone, requestIp, ct);
    }
}
