using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/maps")]
public sealed class MapsController(
    IMapSyncService mapSyncService,
    ILocalizationService localizer,
    AppDbContext db) : ControllerBase
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

    /// <summary>
    /// [SEED-ONLY] Tạo 1 map trống nếu chưa tồn tại. Dùng để fix nhanh khi app hardcode mapId=1
    /// mà DB chưa có MapId đó. Idempotent — gọi nhiều lần không lỗi.
    /// POST /api/v1/maps/seed/1?floorId=1  → đảm bảo có MapId=1 cho FloorId=1.
    /// </summary>
    [HttpPost("seed/{mapId:int}")]
    public async Task<ActionResult> SeedMap(
        int mapId,
        [FromQuery] int floorId = 1,
        [FromQuery] string? mapName = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Maps.AnyAsync(m => m.MapId == mapId, cancellationToken);
        if (exists)
            return Ok(new { seeded = false, mapId, message = "Already exists" });

        // Kiểm tra Floor tồn tại — nếu không thì tạo mới luôn cho seed liền mạch
        var floorExists = await db.Floors.AnyAsync(f => f.FloorId == floorId, cancellationToken);
        if (!floorExists)
        {
            db.Floors.Add(new SmartMarketBot.Domain.Entities.Floor
            {
                FloorId = floorId,
                FloorNumber = floorId
            });
        }

        // IDENTITY_INSERT ON/OFF để insert MapId chỉ định (default ON bị tắt vì MapId là identity).
        // An toàn vì ta kiểm tra AnyAsync(mapId) == false trước → tránh duplicate PK exception.
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [MAP] ON", cancellationToken);
            db.Maps.Add(new SmartMarketBot.Domain.Entities.Map
            {
                MapId = mapId,
                FloorId = floorId,
                MapName = mapName ?? $"Map {mapId} (seeded)",
                WidthMeters = 10.0,
                HeightMeters = 10.0,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [MAP] OFF", cancellationToken);
        }
        catch
        {
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [MAP] OFF", cancellationToken);
            throw;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        return Ok(new { seeded = true, mapId, floorId });
    }

    /// <summary>Upload ảnh mặt bằng (JPG/PNG) do Tablet SLAM sinh ra. Lưu vào thư mục cục bộ của container, trả về đường link làm Background cho Web Manager.</summary>
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
