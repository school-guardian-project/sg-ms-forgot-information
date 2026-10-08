using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Validation;

namespace ms_forgot_information.Api.Password.Infrastructure.Controller;

[ApiController]
[Route("api/v1/password")]
public class PasswordController(
    IForgotPasswordUseCase forgotPasswordUseCase,
    IVerifyPasswordResetCodeUseCase verifyPasswordResetCodeUseCase,
    IResetPasswordUseCase resetPasswordUseCase,
    ILogger<PasswordController> logger) : ControllerBase
{
    [HttpPost("forgot")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequestDto dto, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        logger.LogInformation("Forgot request received for {Email}", EmailLogMask.Mask(dto.Email));
        await forgotPasswordUseCase.ExecuteAsync(dto, ip, ct);

        return Accepted(new { message = "Enviamos un código de verificación a tu correo." });
    }

    [HttpPost("forgot/verify")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> VerifyForgotCode([FromBody] VerifyPasswordResetCodeRequestDto dto, CancellationToken ct)
    {
        var result = await verifyPasswordResetCodeUseCase.ExecuteAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("reset")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequestDto dto, CancellationToken ct)
    {
        await resetPasswordUseCase.ExecuteAsync(dto, ct);
        return Ok(new { message = "Contraseña actualizada correctamente." });
    }
}
