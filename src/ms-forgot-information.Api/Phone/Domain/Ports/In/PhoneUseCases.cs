using ms_forgot_information.Api.Phone.Application.Dto;

namespace ms_forgot_information.Api.Phone.Domain.Ports.In;

public interface IRequestPhoneChangeUseCase
{
    Task ExecuteAsync(Guid profileId, RequestPhoneChangeDto dto, string requestIp, CancellationToken ct);
}

public interface IConfirmPhoneChangeUseCase
{
    Task ExecuteAsync(Guid profileId, ConfirmPhoneChangeDto dto, CancellationToken ct);
}
