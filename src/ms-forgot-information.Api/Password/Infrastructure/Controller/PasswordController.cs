using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Infrastructure.Security;

namespace ms_forgot_information.Api.Password.Infrastructure.Controller;

[ApiController]
[Route("api/v1/password")]
public class PasswordController(
    IForgotPasswordUseCase forgotPasswordUseCase,
    IVerifyPasswordResetCodeUseCase verifyPasswordResetCodeUseCase,
    IResetPasswordUseCase resetPasswordUseCase,
    IRequestPasswordChangeCodeUseCase requestPasswordChangeCodeUseCase,
    IChangePasswordUseCase changePasswordUseCase) : ControllerBase
{
    [HttpPost("forgot")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequestDto dto, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await forgotPasswordUseCase.ExecuteAsync(dto, ip, ct);

        return Accepted(new { message = "Si el correo existe, enviaremos un código de verificación." });
    }

    [HttpPost("forgot/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> VerifyForgotCode([FromBody] VerifyPasswordResetCodeRequestDto dto, CancellationToken ct)
    {
        var result = await verifyPasswordResetCodeUseCase.ExecuteAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("reset")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequestDto dto, CancellationToken ct)
    {
        await resetPasswordUseCase.ExecuteAsync(dto, ct);
        return Ok(new { message = "Contraseña actualizada correctamente." });
    }

    [HttpPost("change/request-code")]
    [Authorize]
    public async Task<IActionResult> RequestChangeCode(CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await requestPasswordChangeCodeUseCase.ExecuteAsync(User.GetProfileId(), ip, ct);
        return Accepted(new { message = "Código enviado a tu correo registrado." });
    }

    [HttpPost("change")]
    [Authorize]
    public async Task<IActionResult> Change([FromBody] ChangePasswordRequestDto dto, CancellationToken ct)
    {
        await changePasswordUseCase.ExecuteAsync(User.GetProfileId(), dto, ct);
        return Ok(new { message = "Contraseña actualizada correctamente." });
    }
}
