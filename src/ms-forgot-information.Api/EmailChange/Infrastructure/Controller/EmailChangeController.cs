using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_forgot_information.Api.EmailChange.Application.Dto;
using ms_forgot_information.Api.EmailChange.Domain.Ports.In;

namespace ms_forgot_information.Api.EmailChange.Infrastructure.Controller;

[ApiController]
[Route("api/v1/email/change")]
public class EmailChangeController(
    IRequestEmailChangeUseCase requestUseCase,
    IVerifyEmailChangeCodeUseCase verifyUseCase,
    ISubmitNewEmailUseCase submitNewEmailUseCase,
    IResendNewEmailUseCase resendNewEmailUseCase,
    IConfirmEmailChangeUseCase confirmUseCase) : ControllerBase
{
    private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    [HttpPost("request")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> RequestCode([FromBody] RequestEmailChangeDto dto, CancellationToken ct)
    {
        await requestUseCase.ExecuteAsync(dto, Ip, ct);
        return Accepted(new { message = "Enviamos un código de verificación a tu correo." });
    }

    [HttpPost("verify")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> Verify([FromBody] VerifyEmailChangeCodeDto dto, CancellationToken ct) =>
        Ok(await verifyUseCase.VerifyCurrentAsync(dto, ct));

    [HttpPost("new")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> SubmitNew([FromBody] SubmitNewEmailDto dto, CancellationToken ct)
    {
        await submitNewEmailUseCase.ExecuteAsync(dto, Ip, ct);
        return Accepted(new { message = "Enviamos un código al nuevo correo." });
    }

    [HttpPost("resend-new")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> ResendNew([FromBody] ResendNewEmailDto dto, CancellationToken ct)
    {
        await resendNewEmailUseCase.ExecuteAsync(dto, Ip, ct);
        return Accepted(new { message = "Enviamos un nuevo código al nuevo correo." });
    }

    [HttpPost("verify-new")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> VerifyNew([FromBody] VerifyEmailChangeCodeDto dto, CancellationToken ct) =>
        Ok(await verifyUseCase.VerifyNewAsync(dto, ct));

    [HttpPost("confirm")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmEmailChangeDto dto, CancellationToken ct)
    {
        await confirmUseCase.ExecuteAsync(dto, ct);
        return Ok(new { message = "Correo actualizado correctamente." });
    }
}
