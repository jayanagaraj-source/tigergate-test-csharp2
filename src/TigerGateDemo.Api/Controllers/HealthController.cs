using Microsoft.AspNetCore.Mvc;

namespace TigerGateDemo.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        startedAt = StartedAt,
        uptimeSeconds = (int)(DateTimeOffset.UtcNow - StartedAt).TotalSeconds,
        version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "unknown"
    });
}
