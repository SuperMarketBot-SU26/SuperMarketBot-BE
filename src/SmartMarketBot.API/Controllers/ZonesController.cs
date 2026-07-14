using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Cung cấp dữ liệu Zone, Aisle và mật độ kệ hàng (shelf density)
/// để Web Manager và Mobile App render dropdown, filter và heat-map bản đồ.
/// </summary>
[ApiController]
[Route("api/v1")]
[AllowAnonymous]
public sealed class ZonesController(IZoneAisleService zoneAisleService) : ControllerBase
{
    // ─── Zone ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy toàn bộ Zone.
    /// Hỗ trợ lọc theo tầng (floorId).
    /// GET /api/v1/zones?floorId=1
    /// </summary>
    [HttpGet("zones")]
    [ProducesResponseType(typeof(IReadOnlyList<ZoneDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ZoneDto>>> GetZones(
        [FromQuery] int? floorId = null,
        CancellationToken ct = default)
    {
        var zones = await zoneAisleService.GetZonesAsync(floorId, ct);
        return Ok(zones);
    }

    // ─── Aisle ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy toàn bộ Aisle (dãy kệ hàng).
    /// Hỗ trợ lọc theo Zone (zoneId).
    /// GET /api/v1/aisles?zoneId=2
    /// </summary>
    [HttpGet("aisles")]
    [ProducesResponseType(typeof(IReadOnlyList<AisleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AisleDto>>> GetAisles(
        [FromQuery] int? zoneId = null,
        CancellationToken ct = default)
    {
        var aisles = await zoneAisleService.GetAislesAsync(zoneId, ct);
        return Ok(aisles);
    }

    // ─── Shelf Density ────────────────────────────────────────────────────────

    /// <summary>
    /// Mật độ hàng hoá (DensityPercentage, EmptyPercentage) của từng kệ,
    /// dựa trên lần AisleScan gần nhất của Robot.
    /// Trả kèm DensityColor (green / yellow / red) để frontend tô màu ngay.
    ///   green  = density ≥ 70 % (kệ đầy hàng)
    ///   yellow = density 40–69 % (sắp hết hàng)
    ///   red    = density &lt; 40 % (cần bổ sung ngay)
    /// GET /api/v1/aisles/density?zoneId=2
    /// </summary>
    [HttpGet("aisles/density")]
    [ProducesResponseType(typeof(IReadOnlyList<AisleDensityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AisleDensityDto>>> GetAisleDensities(
        [FromQuery] int? zoneId = null,
        CancellationToken ct = default)
    {
        var densities = await zoneAisleService.GetAisleDensitiesAsync(zoneId, ct);
        return Ok(densities);
    }
}
