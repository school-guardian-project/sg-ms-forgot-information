using ms_forgot_information.Api.Email.Application.Dto;

namespace ms_forgot_information.Api.Email.Domain.Ports.In;

public interface IRequestEmailChangeUseCase
{
    Task ExecuteAsync(Guid profileId, RequestEmailChangeDto dto, string requestIp, CancellationToken ct);
}

public interface IConfirmEmailChangeUseCase
{
    Task ExecuteAsync(Guid profileId, ConfirmEmailChangeDto dto, CancellationToken ct);
}
