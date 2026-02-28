using Microsoft.AspNetCore.Mvc;
using TodoApi.Application;
using TodoApi.Models.Requests;

namespace TodoApi.Controllers;

[ApiController]
[Route("api")]
public class AuthController(IAuthService authService, ISessionService sessionService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var header = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }

        var token = header["Bearer ".Length..].Trim();
        await sessionService.RemoveSessionAsync(token, cancellationToken);
        return NoContent();
    }
}
