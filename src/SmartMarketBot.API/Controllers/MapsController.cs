using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/maps")]
public sealed class MapsController(
    IMapSyncService mapSyncService,
    ILocalizationService localizer) : ControllerBase
{
    /// <summary>Nhấn "Lưu lên Server" trên Web Manager → đẩy toàn bộ JSON (Nodes, Edges, Shapes) lên đây. BE tự Insert/Update vào DB.</summary>
    [HttpPost("sync")]
    public async Task<ActionResult<MapSyncResponseDto>> SyncMap(
        [FromBody] MapSyncRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.FloorId <= 0)
            return BadRequest(new { message = localizer.Get("FloorIdRequired") });

        if (request.Nodes.Count == 0 && request.Edges.Count == 0 && request.SemanticObjects.Count == 0)
            return BadRequest(new { message = localizer.Get("MapSyncEmptyPayload") });

        var result = await mapSyncService.SyncMapAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Mỗi khi Admin mở Web Manager (hoặc F5) → gọi API này tải map mới nhất từ DB xuống vẽ ra màn hình.</summary>
    [HttpGet("latest")]
    public async Task<ActionResult<MapFloorplanDto>> GetLatestMap(
        [FromQuery] int floorId,
        CancellationToken cancellationToken)
    {
        if (floorId <= 0)
            return BadRequest(new { message = localizer.Get("FloorIdRequired") });

        var map = await mapSyncService.GetLatestMapAsync(floorId, cancellationToken);
        if (map is null)
            return NotFound(new { message = localizer.Get("MapNotFoundForFloor", floorId) });

        return Ok(map);
    }

    /// <summary>Lấy thống kê map: số nodes, edges, semantic objects, thời điểm sync cuối.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<MapSyncStatsDto>> GetMapStats(
        [FromQuery] int floorId,
        CancellationToken cancellationToken)
    {
        if (floorId <= 0)
            return BadRequest(new { message = localizer.Get("FloorIdRequired") });

        var stats = await mapSyncService.GetMapStatsAsync(floorId, cancellationToken);
        return Ok(stats);
    }

    /// <summary>Upload ảnh mặt bằng (JPG/PNG) do Tablet SLAM sinh ra. Lưu vào Supabase Storage, trả về đường link làm Background cho Web Manager.</summary>
    [HttpPost("{mapId:int}/upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadFloorplanImageResponseDto>> UploadFloorplanImage(
        int mapId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = localizer.Get("FileRequired") });

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = localizer.Get("ImageOnlyAllowed") });

        await using var stream = file.OpenReadStream();
        var result = await mapSyncService.UploadFloorplanImageAsync(mapId, stream, file.FileName, cancellationToken);
        return Ok(result);
    }
}
