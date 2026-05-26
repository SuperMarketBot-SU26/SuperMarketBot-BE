using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.ShelfScans;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShelfScansController(IShelfScanService shelfScanService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShelfScanDto>>> GetRecent([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var scans = await shelfScanService.GetRecentScansAsync(take, cancellationToken);
        return Ok(scans);
    }

    [HttpPost]
    public async Task<ActionResult<ShelfScanDto>> Create([FromBody] CreateShelfScanRequestDto request, CancellationToken cancellationToken = default)
    {
        var scan = await shelfScanService.CreateScanAsync(request, cancellationToken);
        return Ok(scan);
    }
}
