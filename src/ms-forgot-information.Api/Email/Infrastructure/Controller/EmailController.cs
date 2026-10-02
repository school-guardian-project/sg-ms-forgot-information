using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ms_forgot_information.Api.Email.Application.Dto;
using ms_forgot_information.Api.Email.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Infrastructure.Security;

namespace ms_forgot_information.Api.Email.Infrastructure.Controller;

[ApiController]
[Route("api/v1/email")]
[Authorize]
public class EmailController(
    IRequestEmailChangeUseCase requestEmailChangeUseCase,
    IConfirmEmailChangeUseCase confirmEmailChangeUseCase) : ControllerBase
{
    [HttpPost("change/request")]
    public async Task<IActionResult> RequestChange([FromBody] RequestEmailChangeDto dto, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await requestEmailChangeUseCase.ExecuteAsync(User.GetProfileId(), dto, ip, ct);
        return Accepted(new { message = "Código enviado al nuevo correo." });
    }

    [HttpPost("change/confirm")]
    public async Task<IActionResult> ConfirmChange([FromBody] ConfirmEmailChangeDto dto, CancellationToken ct)
    {
        await confirmEmailChangeUseCase.ExecuteAsync(User.GetProfileId(), dto, ct);
        return Ok(new { message = "Correo actualizado correctamente." });
    }
}
