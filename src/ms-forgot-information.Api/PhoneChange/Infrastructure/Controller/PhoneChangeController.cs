using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_forgot_information.Api.PhoneChange.Application.Dto;
using ms_forgot_information.Api.PhoneChange.Domain.Ports.In;

namespace ms_forgot_information.Api.PhoneChange.Infrastructure.Controller;

[ApiController]
[Route("api/v1/phone/change")]
public class PhoneChangeController(
    IRequestPhoneChangeUseCase requestUseCase,
    IVerifyPhoneChangeIdentityUseCase verifyIdentityUseCase,
    IRequestPhoneVerificationUseCase requestVerificationUseCase,
    IResendPhoneVerificationUseCase resendVerificationUseCase,
    ICheckPhoneVerificationUseCase checkVerificationUseCase) : ControllerBase
{
    private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    [HttpPost("request")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> RequestIdentityCode([FromBody] RequestPhoneChangeDto dto, CancellationToken ct)
    {
        await requestUseCase.ExecuteAsync(dto, Ip, ct);
        return Accepted(new { message = "Enviamos un código por SMS al teléfono actual." });
    }

    [HttpPost("verify")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> VerifyIdentity([FromBody] VerifyPhoneChangeIdentityDto dto, CancellationToken ct) =>
        Ok(await verifyIdentityUseCase.ExecuteAsync(dto, ct));

    [HttpPost("verification/request")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> RequestVerification([FromBody] RequestPhoneVerificationDto dto, CancellationToken ct)
    {
        await requestVerificationUseCase.ExecuteAsync(dto, Ip, ct);
        return Accepted(new { message = "Enviamos un código por SMS al nuevo teléfono." });
    }

    [HttpPost("verification/resend")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendPhoneVerificationDto dto, CancellationToken ct)
    {
        await resendVerificationUseCase.ExecuteAsync(dto, Ip, ct);
        return Accepted(new { message = "Enviamos un nuevo código por SMS al nuevo teléfono." });
    }

    [HttpPost("verification/check")]
    [EnableRateLimiting("otp-public")]
    public async Task<IActionResult> CheckVerification([FromBody] CheckPhoneVerificationDto dto, CancellationToken ct)
    {
        await checkVerificationUseCase.ExecuteAsync(dto, ct);
        return Ok(new { message = "Teléfono actualizado correctamente." });
    }
}