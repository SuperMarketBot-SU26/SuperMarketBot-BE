using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.ShelfScans;
using SmartMarketBot.Application.Models.Staff;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/shelf-scans")]
public sealed class ShelfScansController(
    IShelfScanService shelfScanService,
    IStaffService staffService) : ControllerBase
{
    /// <summary>Lấy danh sách quét kệ gần nhất.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShelfScanDto>>> GetRecent(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var scans = await shelfScanService.GetRecentScansAsync(take, cancellationToken);
        return Ok(scans);
    }

    /// <summary>Tạo bản ghi quét kệ mới (Robot gọi sau khi chụp ảnh AI Vision).</summary>
    [HttpPost]
    public async Task<ActionResult<ShelfScanDto>> Create(
        [FromBody] CreateShelfScanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var scan = await shelfScanService.CreateScanAsync(request, cancellationToken);
        return Ok(scan);
    }

    /// <summary>
    /// Flow 4 — Báo cáo kệ trống / bị che khuất (Robot hoặc khách hàng gọi).
    /// Backend kiểm tra tổng tồn kho và tự động gửi cảnh báo đến nhân viên gần nhất.
    /// </summary>
    [HttpPost("report-oos")]
    public async Task<ActionResult<ReportOosResponseDto>> ReportOutOfStock(
        [FromBody] ReportOosRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await staffService.ReportOutOfStockAsync(request, cancellationToken);
        return Ok(result);
    }
}
