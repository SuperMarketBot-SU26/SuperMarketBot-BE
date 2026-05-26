namespace SmartMarketBot.Application.Models.ShelfScans;

public sealed record ShelfScanDto(
    int ScanId,
    int AisleId,
    int? ShelfLevelId,
    int RobotId,
    DateTime ScannedAt,
    decimal EmptyPercentage,
    bool NeedsRestock,
    string? ImageUrl,
    string? AiResponseRaw);

public sealed record CreateShelfScanRequestDto(
    int AisleId,
    int? ShelfLevelId,
    int RobotId,
    decimal EmptyPercentage,
    string? ImageUrl,
    string? AiResponseRaw);
