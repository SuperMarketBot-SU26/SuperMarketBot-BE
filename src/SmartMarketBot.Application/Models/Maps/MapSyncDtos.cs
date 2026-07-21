namespace SmartMarketBot.Application.Models.Maps;

// Input DTOs (properties with defaults) — class syntax for init-only flexibility
public sealed record MapSyncRequestDto
{
    public required int FloorId { get; init; }
    public required string MapName { get; init; }
    public string? MapData { get; init; }
    public double WidthMeters { get; init; }
    public double HeightMeters { get; init; }
    public List<MapSyncNodeDto> Nodes { get; init; } = [];
    public List<MapSyncEdgeDto> Edges { get; init; } = [];
    public List<MapSyncSemanticObjectDto> SemanticObjects { get; init; } = [];
}

public sealed record MapSyncNodeDto(
    int? NodeId,
    string NodeName,
    double XCoord,
    double YCoord,
    string NodeType,
    bool IsBlocked);

public sealed record MapSyncEdgeDto(
    int? EdgeId,
    int FromNodeId,
    int ToNodeId,
    double Distance,
    bool IsBidirectional);

public sealed record MapSyncSemanticObjectDto(
    int? ObjectId,
    string ObjectType,
    double XMin,
    double YMin,
    double XMax,
    double YMax,
    string? Label,
    double? Confidence,
    DateTime? DetectedAt,
    string? ImageUrl);

// Response DTOs (positional constructors)
public sealed record MapSyncResponseDto(
    int MapId,
    int NodesCreated,
    int NodesUpdated,
    int EdgesCreated,
    int EdgesUpdated,
    int SemanticObjectsCreated,
    int SemanticObjectsUpdated,
    int NodesDeleted,
    int EdgesDeleted,
    int SemanticObjectsDeleted,
    string Message);

public sealed record MapFloorplanDto(
    int MapId,
    int FloorId,
    string MapName,
    DateTime CreatedAt,
    string? FloorplanImageUrl,
    double WidthMeters,
    double HeightMeters,
    List<MapSyncNodeDto> Nodes,
    List<MapSyncEdgeDto> Edges,
    List<MapSyncSemanticObjectDto> SemanticObjects);

public sealed record MapSyncStatsDto(
    int TotalNodes,
    int TotalEdges,
    int TotalSemanticObjects,
    DateTime? LastSyncedAt,
    int? MapId);

public sealed record UploadFloorplanImageResponseDto(
    int MapId,
    string ImageUrl,
    string Message);

public sealed record MapSummaryDto(
    int MapId,
    int FloorId,
    string MapName,
    DateTime CreatedAt,
    string? FloorplanImageUrl,
    double WidthMeters,
    double HeightMeters,
    int NodeCount,
    int EdgeCount,
    int SemanticObjectCount);
