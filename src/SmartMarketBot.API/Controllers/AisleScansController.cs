using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.AisleScans;
using SmartMarketBot.Application.Models.Staff;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/shelf-scans")]
public sealed class AisleScansController(
    IAisleScanService AisleScanService,
    IStaffService staffService) : ControllerBase
{
    /// <summary>Lấy danh sách quét kệ gần nhất.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShelfScanDto>>> GetRecent(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var scans = await AisleScanService.GetRecentScansAsync(take, cancellationToken);
        return Ok(scans);
    }

    /// <summary>
    /// Tạo bản ghi quét kệ mới.
    /// Client có thể gửi kèm ImageBase64 (sẽ upload Cloudinary và dùng AI phân tích mật độ còn hàng)
    /// hoặc gửi sẵn ImageUrl. Nếu không có ảnh, BE sẽ dùng dữ liệu kệ làm fallback.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ShelfScanDto>> Create(
        [FromBody] CreateAisleScanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { message = "Body không được rỗng." });
        if (request.AisleId <= 0 || request.RobotId <= 0)
            return BadRequest(new { message = "AisleId và RobotId phải > 0." });

        var scan = await AisleScanService.CreateScanAsync(request, cancellationToken);
        return Ok(scan);
    }

    /// <summary>
    /// Robot chụp ảnh tại một aisle node rồi gửi về.
    /// Backend sẽ upload ảnh, dùng AI phân tích mật độ còn hàng, lưu lịch sử scan và gắn aisleNodeId + aisleId.
    /// </summary>
    [HttpPost("scan-with-photo")]
    [Consumes("application/json")]
    public async Task<ActionResult<ShelfScanDto>> ScanWithPhoto(
        [FromBody] AisleScanWithPhotoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { message = "Body không được rỗng." });
        if (request.AisleId <= 0 || request.RobotId <= 0)
            return BadRequest(new { message = "AisleId và RobotId phải > 0." });
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest(new { message = "ImageBase64 không được rỗng." });

        var create = new CreateAisleScanRequestDto(
            request.AisleId,
            ShelfLevelId: null,
            request.RobotId,
            EmptyPercentage: null,
            ImageBase64: request.ImageBase64,
            ImageUrl: null,
            AisleNodeId: request.AisleNodeId);

        var scan = await AisleScanService.CreateScanAsync(create, cancellationToken);
        return Ok(scan);
    }

    /// <summary>
    /// Endpoint riêng cho flow robot dừng tại aisle node: lưu lịch sử scan kèm aisleNodeId và aisleId.
    /// </summary>
    [HttpPost("robot-node-scan")]
    [Consumes("application/json")]
    public async Task<ActionResult<ShelfScanDto>> ScanAtAisleNode(
        [FromBody] AisleScanWithPhotoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { message = "Body không được rỗng." });
        if (request.AisleId <= 0 || request.RobotId <= 0)
            return BadRequest(new { message = "AisleId và RobotId phải > 0." });
        if (request.AisleNodeId is null or <= 0)
            return BadRequest(new { message = "AisleNodeId phải > 0." });
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest(new { message = "ImageBase64 không được rỗng." });

        var create = new CreateAisleScanRequestDto(
            request.AisleId,
            ShelfLevelId: null,
            request.RobotId,
            EmptyPercentage: null,
            ImageBase64: request.ImageBase64,
            ImageUrl: null,
            AisleNodeId: request.AisleNodeId);

        var scan = await AisleScanService.CreateScanAsync(create, cancellationToken);
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
