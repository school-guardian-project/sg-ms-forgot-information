using ms_forgot_information.Api.Password.Application.Dto;

namespace ms_forgot_information.Api.Password.Domain.Ports.In;

public interface IForgotPasswordUseCase
{
    Task ExecuteAsync(ForgotPasswordRequestDto dto, string requestIp, CancellationToken ct);
}

public interface IVerifyPasswordResetCodeUseCase
{
    Task<VerifyPasswordResetCodeResponseDto> ExecuteAsync(VerifyPasswordResetCodeRequestDto dto, CancellationToken ct);
}

public interface IResetPasswordUseCase
{
    Task ExecuteAsync(ResetPasswordRequestDto dto, CancellationToken ct);
}
