using ITender.Integration.Api.Middleware;
using ITender.Integration.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ITender.Integration.Api.Controllers;

public sealed record TokenRequest(string? Username, string? Password);

[ApiController]
[Route("api/v1/Authentication")]
[AllowAnonymous]
public sealed class AuthenticationController(ICredentialAuthService auth) : ControllerBase
{
    /// <summary>Exchange CRM credentials for a short-lived bearer token.</summary>
    [HttpPost("authenticate")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Token([FromBody] TokenRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        // OAuth-style responses must never be cached
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, correlationId, message = "Username and password are required." });

        var result = await auth.AuthenticateAsync(request.Username, request.Password, correlationId, ct);

        if (!result.Success)
            return StatusCode(result.HttpStatus, new
            {
                success = false,
                correlationId,
                message = result.Message,
                requiresPasswordChange = result.RequiresPasswordChange
            });

        return Ok(new
        {
            success = true,
            accessToken = result.AccessToken,
            tokenType = "Bearer",
            expiresIn = result.ExpiresIn
        });
    }
}
