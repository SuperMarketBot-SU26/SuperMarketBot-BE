using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.RobotRoutes;
using SmartMarketBot.Domain.Enums;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// CRUD cho RobotRoute: phân loại theo Zone, quản lý waypoints (RouteNodeMapping).
/// Không chịu trách nhiệm gửi lệnh xuống robot — chỉ cung cấp dữ liệu route.
/// </summary>
public sealed class RobotRouteService(
    AppDbContext dbContext,
    ILocalizationService localizer,
    ILogger<RobotRouteService> logger) : IRobotRouteService
{
    public async Task<IReadOnlyList<RobotRouteListDto>> GetRoutesByMapAsync(
        int mapId,
        int? zoneId = null,
        string? routeType = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.RobotRoutes
            .AsNoTracking()
            .Where(r => r.MapId == mapId);

        if (zoneId.HasValue)
            query = query.Where(r => r.ZoneId == zoneId.Value);

        // Validate routeType query param against enum. Unknown string → empty list
        // (matches FE's expectation that invalid filter just shows nothing, instead
        // of throwing 400 on every map-load if FE cached a stale value).
        if (!string.IsNullOrWhiteSpace(routeType))
        {
            if (RouteTypeKindExtensions.TryParseDbString(routeType, out var kind) && Enum.IsDefined(kind))
                query = query.Where(r => r.RouteType == kind);
            else
                return [];
        }

        var routes = await query
            .Include(r => r.Zone)
            .Include(r => r.RouteNodeMappings)
            .OrderBy(r => r.RouteName)
            .ToListAsync(cancellationToken);

        return routes.Select(r => new RobotRouteListDto(
            r.RobotRouteId,
            r.MapId,
            r.RouteName,
            r.RouteType.ToDbString(),
            r.Description,
            r.ZoneId,
            r.Zone?.ZoneName,
            r.RobotId,
            r.CreatedAt,
            r.RouteNodeMappings.Count))
            .ToList();
    }

    public async Task<RobotRouteDetailDto?> GetRouteByIdAsync(
        int routeId,
        CancellationToken cancellationToken = default)
    {
        var route = await dbContext.RobotRoutes
            .AsNoTracking()
            .Include(r => r.Zone)
            .Include(r => r.RouteNodeMappings.OrderBy(m => m.SequenceOrder))
                .ThenInclude(m => m.Node)
            .FirstOrDefaultAsync(r => r.RobotRouteId == routeId, cancellationToken);

        if (route is null)
            return null;

        var waypoints = route.RouteNodeMappings
            .Select(m => new RouteWaypointDto(
                m.NodeId,
                m.Node?.NodeName,
                m.Node?.XCoord ?? 0,
                m.Node?.YCoord ?? 0,
                m.SequenceOrder))
            .ToList();

        return new RobotRouteDetailDto(
            route.RobotRouteId,
            route.MapId,
            route.RouteName,
            route.RouteType.ToDbString(),
            route.Description,
            route.ZoneId,
            route.Zone?.ZoneName,
            route.RobotId,
            route.CreatedAt,
            waypoints);
    }

    public async Task<RobotRouteResultDto> CreateRouteAsync(
        RobotRouteCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validate: Map tồn tại
        var mapExists = await dbContext.Maps
            .AsNoTracking()
            .AnyAsync(m => m.MapId == dto.MapId, cancellationToken);
        if (!mapExists)
            throw new KeyNotFoundException(localizer.Get("MapNotFound", dto.MapId));

        // Validate: Robot tồn tại
        var robotExists = await dbContext.Robots
            .AsNoTracking()
            .AnyAsync(r => r.RobotId == dto.RobotId, cancellationToken);
        if (!robotExists)
            throw new KeyNotFoundException(localizer.Get("RobotNotFoundById", dto.RobotId));

        // Validate: Zone hợp lệ (nếu có)
        if (dto.ZoneId.HasValue)
        {
            var zoneExists = await dbContext.Zones
                .AsNoTracking()
                .AnyAsync(z => z.ZoneId == dto.ZoneId.Value, cancellationToken);
            if (!zoneExists)
                throw new KeyNotFoundException(localizer.Get("ZoneNotFound", dto.ZoneId.Value));
        }

        // Validate: RouteType hợp lệ. DTO là string (FE wire format) — chuyển sang enum.
        if (string.IsNullOrWhiteSpace(dto.RouteType))
            throw new InvalidOperationException(localizer.Get("InvalidRouteType",
                dto.RouteType ?? "(null)", string.Join(", ", RouteTypeKindExtensions.AllDbStrings)));
        if (!RouteTypeKindExtensions.TryParseDbString(dto.RouteType, out var routeType) || !Enum.IsDefined(routeType))
            throw new InvalidOperationException(localizer.Get("InvalidRouteType",
                dto.RouteType, string.Join(", ", RouteTypeKindExtensions.AllDbStrings)));

        var route = new Domain.Entities.RobotRoute
        {
            MapId = dto.MapId,
            RobotId = dto.RobotId,
            RouteName = dto.RouteName,
            RouteType = routeType,
            Description = dto.Description,
            ZoneId = dto.ZoneId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.RobotRoutes.Add(route);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Tạo waypoint mappings nếu có NodeIds
        if (dto.NodeIds.Count != 0)
        {
            // Validate tất cả NodeId tồn tại trong Map
            var validNodeIds = await dbContext.NavigationNodes
                .AsNoTracking()
                .Where(n => n.MapId == dto.MapId && dto.NodeIds.Contains(n.NodeId))
                .Select(n => n.NodeId)
                .ToListAsync(cancellationToken);

            var invalidIds = dto.NodeIds.Except(validNodeIds).ToList();
            if (invalidIds.Count != 0)
                throw new InvalidOperationException(
                    localizer.Get("Route_InvalidNodeIds", string.Join(", ", invalidIds)));

            for (int i = 0; i < dto.NodeIds.Count; i++)
            {
                dbContext.RouteNodeMappings.Add(new Domain.Entities.RouteNodeMapping
                {
                    RobotRouteId = route.RobotRouteId,
                    NodeId = dto.NodeIds[i],
                    SequenceOrder = i
                });
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("[Route] Created route {RouteId} '{RouteName}' with {Count} waypoints",
            route.RobotRouteId, route.RouteName, dto.NodeIds.Count);

        return new RobotRouteResultDto(
            route.RobotRouteId,
            route.RouteName,
            localizer.Get("Route_Created", route.RouteName));
    }

    public async Task<RobotRouteResultDto> UpdateRouteAsync(
        int routeId,
        RobotRouteUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var route = await dbContext.RobotRoutes
            .Include(r => r.RouteNodeMappings)
            .FirstOrDefaultAsync(r => r.RobotRouteId == routeId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("RouteNotFound", routeId));

        // Validate Zone hợp lệ (nếu có)
        if (dto.ZoneId.HasValue)
        {
            var zoneExists = await dbContext.Zones
                .AsNoTracking()
                .AnyAsync(z => z.ZoneId == dto.ZoneId.Value, cancellationToken);
            if (!zoneExists)
                throw new KeyNotFoundException(localizer.Get("ZoneNotFound", dto.ZoneId.Value));
        }

        // Validate: RouteType hợp lệ
        if (string.IsNullOrWhiteSpace(dto.RouteType))
            throw new InvalidOperationException(localizer.Get("InvalidRouteType",
                dto.RouteType ?? "(null)", string.Join(", ", RouteTypeKindExtensions.AllDbStrings)));
        if (!RouteTypeKindExtensions.TryParseDbString(dto.RouteType, out var routeType) || !Enum.IsDefined(routeType))
            throw new InvalidOperationException(localizer.Get("InvalidRouteType",
                dto.RouteType, string.Join(", ", RouteTypeKindExtensions.AllDbStrings)));

        // Cập nhật thông tin route
        route.RouteName = dto.RouteName;
        route.RouteType = routeType;
        route.Description = dto.Description;
        route.ZoneId = dto.ZoneId;

        // Xóa waypoint cũ, tạo lại từ NodeIds mới
        dbContext.RouteNodeMappings.RemoveRange(route.RouteNodeMappings);

        if (dto.NodeIds.Count != 0)
        {
            var validNodeIds = await dbContext.NavigationNodes
                .AsNoTracking()
                .Where(n => n.MapId == route.MapId && dto.NodeIds.Contains(n.NodeId))
                .Select(n => n.NodeId)
                .ToListAsync(cancellationToken);

            var invalidIds = dto.NodeIds.Except(validNodeIds).ToList();
            if (invalidIds.Count != 0)
                throw new InvalidOperationException(
                    localizer.Get("Route_InvalidNodeIds", string.Join(", ", invalidIds)));

            for (int i = 0; i < dto.NodeIds.Count; i++)
            {
                dbContext.RouteNodeMappings.Add(new Domain.Entities.RouteNodeMapping
                {
                    RobotRouteId = route.RobotRouteId,
                    NodeId = dto.NodeIds[i],
                    SequenceOrder = i
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Route] Updated route {RouteId} '{RouteName}' with {Count} waypoints",
            route.RobotRouteId, route.RouteName, dto.NodeIds.Count);

        return new RobotRouteResultDto(
            route.RobotRouteId,
            route.RouteName,
            localizer.Get("Route_Updated", route.RouteName));
    }

    public async Task<bool> DeleteRouteAsync(int routeId, CancellationToken cancellationToken = default)
    {
        var route = await dbContext.RobotRoutes
            .FirstOrDefaultAsync(r => r.RobotRouteId == routeId, cancellationToken);

        if (route is null)
            return false;

        dbContext.RobotRoutes.Remove(route);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Route] Deleted route {RouteId} '{RouteName}'",
            route.RobotRouteId, route.RouteName);

        return true;
    }
}
