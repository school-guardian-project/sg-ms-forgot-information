using ms_forgot_information.Api.Email.Application.Dto;
using ms_forgot_information.Api.Email.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;

namespace ms_forgot_information.Api.Email.Application.UseCase;

public class ConfirmEmailChangeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService,
    IDomainEventPublisher eventPublisher) : IConfirmEmailChangeUseCase
{
    public async Task ExecuteAsync(Guid profileId, ConfirmEmailChangeDto dto, CancellationToken ct)
    {
        var ticket = await verificationCodeService.VerifyAsync(profileId, Purpose.ChangeEmail, dto.Code, ct);

        try
        {
            await identityDirectory.UpdateEmailAsync(profileId, ticket.Target, ct);
        }
        catch (Exception ex) when (ex is not VerificationException)
        {
            throw new UpstreamUpdateException(string.Empty, ex);
        }

        await verificationCodeService.CompleteAsync(ticket.RequestId, ct);

        await eventPublisher.PublishAsync(
            "UserEmailChanged",
            "forgot-information.email.changed",
            new { profileId, occurredAt = DateTime.UtcNow },
            ct);
    }
}
