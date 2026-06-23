using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class MapSyncService(
    AppDbContext db,
    IFileStorageService fileStorage,
    ILocalizationService localizer,
    ILogger<MapSyncService> logger) : IMapSyncService
{
    private const string FloorplanFolder = "floorplans";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<MapSyncResponseDto> SyncMapAsync(MapSyncRequestDto request, CancellationToken cancellationToken = default)
    {
        var map = await db.Maps
            .FirstOrDefaultAsync(m => m.FloorId == request.FloorId, cancellationToken);

        if (map is null)
        {
            map = new Map
            {
                FloorId = request.FloorId,
                MapName = request.MapName,
                MapData = request.MapData ?? JsonSerializer.Serialize(new { floorId = request.FloorId }, JsonOptions),
                CreatedAt = DateTime.UtcNow
            };
            db.Maps.Add(map);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            map.MapName = request.MapName;
            map.MapData = request.MapData ?? map.MapData;
        }

        var stats = await SyncNodesAsync(map.MapId, request.Nodes, cancellationToken);
        var edgeStats = await SyncEdgesAsync(map.MapId, request.Edges, cancellationToken);
        var soStats = await SyncSemanticObjectsAsync(map.MapId, request.SemanticObjects, cancellationToken);

        logger.LogInformation(
            "Map synced: MapId={MapId}, FloorId={FloorId}, Nodes={Nodes}/{Edges}, Semantics={Semantics}",
            map.MapId, request.FloorId, stats.nodesCreated + stats.nodesUpdated, edgeStats.nodesCreated + edgeStats.nodesUpdated, soStats.created + soStats.updated);

        return new MapSyncResponseDto(
            map.MapId,
            stats.nodesCreated, stats.nodesUpdated,
            edgeStats.nodesCreated, edgeStats.nodesUpdated,
            soStats.created, soStats.updated,
            stats.nodesDeleted, edgeStats.nodesDeleted, soStats.deleted,
            localizer.Get("MapSyncSuccess"));
    }

    private async Task<(int nodesCreated, int nodesUpdated, int nodesDeleted)> SyncNodesAsync(
        int mapId, List<MapSyncNodeDto> nodes, CancellationToken cancellationToken)
    {
        var existingNodes = await db.NavigationNodes
            .Where(n => n.MapId == mapId)
            .ToDictionaryAsync(n => n.NodeId, cancellationToken);

        int created = 0, updated = 0, deleted = 0;

        var incomingIds = nodes
            .Where(n => n.NodeId.HasValue)
            .Select(n => n.NodeId!.Value)
            .ToHashSet();

        var nodesToDelete = existingNodes.Keys
            .Except(incomingIds)
            .ToList();

        if (nodesToDelete.Count > 0)
        {
            var toRemove = await db.NavigationNodes
                .Where(n => n.MapId == mapId && nodesToDelete.Contains(n.NodeId))
                .ToListAsync(cancellationToken);
            db.NavigationNodes.RemoveRange(toRemove);
            deleted = toRemove.Count;
        }

        foreach (var dto in nodes)
        {
            if (dto.NodeId.HasValue && existingNodes.TryGetValue(dto.NodeId.Value, out var existing))
            {
                existing.NodeName = dto.NodeName;
                existing.XCoord = dto.XCoord;
                existing.YCoord = dto.YCoord;
                existing.NodeType = dto.NodeType;
                existing.IsBlocked = dto.IsBlocked;
                updated++;
            }
            else
            {
                db.NavigationNodes.Add(new NavigationNode
                {
                    MapId = mapId,
                    NodeName = dto.NodeName,
                    XCoord = dto.XCoord,
                    YCoord = dto.YCoord,
                    NodeType = dto.NodeType,
                    IsBlocked = dto.IsBlocked
                });
                created++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return (created, updated, deleted);
    }

    private async Task<(int nodesCreated, int nodesUpdated, int nodesDeleted)> SyncEdgesAsync(
        int mapId, List<MapSyncEdgeDto> edges, CancellationToken cancellationToken)
    {
        int created = 0, updated = 0, deleted = 0;

        if (edges.Count == 0)
        {
            var allExistingEdgesForMap = await db.NavigationEdges
                .Where(e => db.NavigationNodes.Any(n => n.NodeId == e.FromNodeId && n.MapId == mapId))
                .ToListAsync(cancellationToken);
            if (allExistingEdgesForMap.Count > 0)
            {
                db.NavigationEdges.RemoveRange(allExistingEdgesForMap);
                deleted = allExistingEdgesForMap.Count;
                await db.SaveChangesAsync(cancellationToken);
            }
            return (created, updated, deleted);
        }

        var nodeIdsInMap = await db.NavigationNodes
            .Where(n => n.MapId == mapId)
            .Select(n => n.NodeId)
            .ToHashSetAsync(cancellationToken);

        var validEdges = edges
            .Where(e => nodeIdsInMap.Contains(e.FromNodeId) && nodeIdsInMap.Contains(e.ToNodeId))
            .ToList();

        var existingEdges = await db.NavigationEdges
            .Where(e => nodeIdsInMap.Contains(e.FromNodeId))
            .ToDictionaryAsync(e => (e.FromNodeId, e.ToNodeId), cancellationToken);

        var incomingKeySet = validEdges
            .Select(e => (e.FromNodeId, e.ToNodeId))
            .ToHashSet();

        var edgesToDelete = existingEdges.Keys
            .Where(k => !incomingKeySet.Contains(k))
            .ToList();

        if (edgesToDelete.Count > 0)
        {
            var toRemove = existingEdges
                .Where(kvp => edgesToDelete.Contains(kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();
            db.NavigationEdges.RemoveRange(toRemove);
            deleted = toRemove.Count;
        }

        foreach (var dto in validEdges)
        {
            if (existingEdges.TryGetValue((dto.FromNodeId, dto.ToNodeId), out var existing))
            {
                existing.Distance = dto.Distance;
                existing.IsBidirectional = dto.IsBidirectional;
                updated++;
            }
            else
            {
                db.NavigationEdges.Add(new NavigationEdge
                {
                    FromNodeId = dto.FromNodeId,
                    ToNodeId = dto.ToNodeId,
                    Distance = dto.Distance,
                    IsBidirectional = dto.IsBidirectional
                });
                created++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return (created, updated, deleted);
    }

    private async Task<(int created, int updated, int deleted)> SyncSemanticObjectsAsync(
        int mapId, List<MapSyncSemanticObjectDto> objects, CancellationToken cancellationToken)
    {
        int created = 0, updated = 0, deleted = 0;

        if (objects.Count == 0)
        {
            var existing = await db.SemanticObjects
                .Where(s => s.MapId == mapId)
                .ToListAsync(cancellationToken);
            if (existing.Count > 0)
            {
                db.SemanticObjects.RemoveRange(existing);
                deleted = existing.Count;
                await db.SaveChangesAsync(cancellationToken);
            }
            return (created, updated, deleted);
        }

        var existingObjects = await db.SemanticObjects
            .Where(s => s.MapId == mapId)
            .ToDictionaryAsync(s => s.ObjectId, cancellationToken);

        var incomingIds = objects
            .Where(o => o.ObjectId.HasValue)
            .Select(o => o.ObjectId!.Value)
            .ToHashSet();

        var objectsToDelete = existingObjects.Keys
            .Except(incomingIds)
            .ToList();

        if (objectsToDelete.Count > 0)
        {
            var toRemove = await db.SemanticObjects
                .Where(s => s.MapId == mapId && objectsToDelete.Contains(s.ObjectId))
                .ToListAsync(cancellationToken);
            db.SemanticObjects.RemoveRange(toRemove);
            deleted = toRemove.Count;
        }

        foreach (var dto in objects)
        {
            if (dto.ObjectId.HasValue && existingObjects.TryGetValue(dto.ObjectId.Value, out var existing))
            {
                existing.ObjectType = dto.ObjectType;
                existing.XMin = dto.XMin;
                existing.YMin = dto.YMin;
                existing.XMax = dto.XMax;
                existing.YMax = dto.YMax;
                existing.Label = dto.Label;
                existing.Confidence = dto.Confidence;
                existing.DetectedAt = dto.DetectedAt;
                existing.ImageUrl = dto.ImageUrl;
                updated++;
            }
            else
            {
                db.SemanticObjects.Add(new SemanticObject
                {
                    MapId = mapId,
                    ObjectType = dto.ObjectType,
                    XMin = dto.XMin,
                    YMin = dto.YMin,
                    XMax = dto.XMax,
                    YMax = dto.YMax,
                    Label = dto.Label,
                    Confidence = dto.Confidence,
                    DetectedAt = dto.DetectedAt,
                    ImageUrl = dto.ImageUrl
                });
                created++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return (created, updated, deleted);
    }

    public async Task<MapFloorplanDto?> GetLatestMapAsync(int floorId, CancellationToken cancellationToken = default)
    {
        var map = await db.Maps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FloorId == floorId, cancellationToken);

        if (map is null)
            return null;

        var nodes = await db.NavigationNodes
            .AsNoTracking()
            .Where(n => n.MapId == map.MapId)
            .Select(n => new MapSyncNodeDto(
                n.NodeId, n.NodeName, n.XCoord, n.YCoord, n.NodeType, n.IsBlocked))
            .ToListAsync(cancellationToken);

        var nodeIds = nodes.Select(n => n.NodeId).ToHashSet();

        var edges = await db.NavigationEdges
            .AsNoTracking()
            .Where(e => nodeIds.Contains(e.FromNodeId))
            .Select(e => new MapSyncEdgeDto(
                e.EdgeId, e.FromNodeId, e.ToNodeId, e.Distance, e.IsBidirectional))
            .ToListAsync(cancellationToken);

        var semanticObjects = await db.SemanticObjects
            .AsNoTracking()
            .Where(s => s.MapId == map.MapId)
            .Select(s => new MapSyncSemanticObjectDto(
                s.ObjectId, s.ObjectType,
                s.XMin, s.YMin, s.XMax, s.YMax,
                s.Label, s.Confidence, s.DetectedAt, s.ImageUrl))
            .ToListAsync(cancellationToken);

        return new MapFloorplanDto(
            map.MapId, map.FloorId, map.MapName, map.CreatedAt,
            map.FloorplanImageUrl,
            nodes, edges, semanticObjects);
    }

    public async Task<MapSyncStatsDto> GetMapStatsAsync(int floorId, CancellationToken cancellationToken = default)
    {
        var map = await db.Maps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FloorId == floorId, cancellationToken);

        if (map is null)
            return new MapSyncStatsDto(0, 0, 0, null, null);

        var nodeCount = await db.NavigationNodes
            .CountAsync(n => n.MapId == map.MapId, cancellationToken);

        var edgeCount = await db.NavigationEdges
            .Where(e => db.NavigationNodes.Any(n => n.NodeId == e.FromNodeId && n.MapId == map.MapId))
            .CountAsync(cancellationToken);

        var semanticCount = await db.SemanticObjects
            .CountAsync(s => s.MapId == map.MapId, cancellationToken);

        return new MapSyncStatsDto(nodeCount, edgeCount, semanticCount, map.CreatedAt, map.MapId);
    }

    public async Task<UploadFloorplanImageResponseDto> UploadFloorplanImageAsync(
        int mapId, Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var map = await db.Maps.FindAsync([mapId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("MapNotFound", mapId));

        var imageUrl = await fileStorage.SaveAsync(stream, fileName, FloorplanFolder, cancellationToken);

        map.FloorplanImageUrl = imageUrl;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Floorplan image uploaded for MapId={MapId}, URL={Url}", mapId, imageUrl);

        return new UploadFloorplanImageResponseDto(mapId, imageUrl, localizer.Get("FloorplanImageUploaded"));
    }
}
