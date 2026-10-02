using ms_forgot_information.Api.Phone.Application.Dto;
using ms_forgot_information.Api.Phone.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;

namespace ms_forgot_information.Api.Phone.Application.UseCase;

public class ConfirmPhoneChangeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    IDomainEventPublisher eventPublisher) : IConfirmPhoneChangeUseCase
{
    public async Task ExecuteAsync(Guid profileId, ConfirmPhoneChangeDto dto, CancellationToken ct)
    {
        var ticket = await verificationCodeService.VerifyAsync(profileId, Purpose.ChangePhone, dto.Code, ct);

        try
        {
            await identityDirectory.UpdatePhoneAsync(profileId, ticket.Target, ct);
        }
        catch (Exception ex) when (ex is not VerificationException)
        {
            throw new UpstreamUpdateException(string.Empty, ex);
        }

        await verificationCodeService.CompleteAsync(ticket.RequestId, ct);

        await eventPublisher.PublishAsync(
            "UserPhoneChanged",
            "forgot-information.phone.changed",
            new { profileId, occurredAt = DateTime.UtcNow },
            ct);
    }
}
