using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Application.Services;

namespace ms_forgot_information.Api.Password.Application.UseCase;

/// <summary>Lets an already-authenticated user who forgot their current password get a code without logging out.</summary>
public class RequestPasswordChangeCodeService(
    IIdentityDirectoryClient identityDirectory,
    VerificationCodeService verificationCodeService) : IRequestPasswordChangeCodeUseCase
{
    public async Task ExecuteAsync(Guid profileId, string requestIp, CancellationToken ct)
    {
        var contact = await identityDirectory.GetContactInfoAsync(profileId, ct);
        await verificationCodeService.IssueAsync(profileId, Purpose.ChangePassword, contact.Email, requestIp, ct);
    }
}
