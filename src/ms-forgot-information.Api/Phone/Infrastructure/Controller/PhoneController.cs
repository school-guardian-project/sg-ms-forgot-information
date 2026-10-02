using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ms_forgot_information.Api.Phone.Application.Dto;
using ms_forgot_information.Api.Phone.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Infrastructure.Security;

namespace ms_forgot_information.Api.Phone.Infrastructure.Controller;

[ApiController]
[Route("api/v1/phone")]
[Authorize]
public class PhoneController(
    IRequestPhoneChangeUseCase requestPhoneChangeUseCase,
    IConfirmPhoneChangeUseCase confirmPhoneChangeUseCase) : ControllerBase
{
    [HttpPost("change/request")]
    public async Task<IActionResult> RequestChange([FromBody] RequestPhoneChangeDto dto, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await requestPhoneChangeUseCase.ExecuteAsync(User.GetProfileId(), dto, ip, ct);
        return Accepted(new { message = "Código enviado al nuevo teléfono." });
    }

    [HttpPost("change/confirm")]
    public async Task<IActionResult> ConfirmChange([FromBody] ConfirmPhoneChangeDto dto, CancellationToken ct)
    {
        await confirmPhoneChangeUseCase.ExecuteAsync(User.GetProfileId(), dto, ct);
        return Ok(new { message = "Teléfono actualizado correctamente." });
    }
}
