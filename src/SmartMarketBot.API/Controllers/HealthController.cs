using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Healthcheck endpoint cho Docker/K8s liveness probe.
/// Trả 200 nếu DB OK + service đang chạy; 503 nếu DB mất kết nối.
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController(AppDbContext db) : ControllerBase
{
    /// <summary>Ping đơn giản — DB roundtrip 1 query.</summary>
    [HttpGet]
    public async Task<ActionResult<object>> Get(CancellationToken ct)
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        var status = canConnect ? "healthy" : "degraded";
        var code = canConnect ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        return StatusCode(code, new
        {
            status,
            service = "SmartMarketBot-BE",
            version = "1.0.0",
            db = canConnect ? "ok" : "down",
            timestamp = DateTime.UtcNow
        });
    }
}