namespace SmartMarketBot.Application.Models.AisleScans;

public sealed record ShelfScanDto(
    int ScanId,
    int AisleId,
    int? ShelfLevelId,
    int RobotId,
    DateTime ScannedAt,
    decimal EmptyPercentage,
    bool NeedsRestock,
    string? ImageUrl);

/// <summary>
/// Request robot/AI Vision tạo bản ghi quét kệ.
/// Có thể gửi ảnh Base64 (sẽ upload Cloudinary) HOẶC sẵn imageUrl.
/// EmptyPercentage có thể bỏ trống — BE tự tính theo Slot.Quantity của kệ đó.
/// </summary>
public sealed record CreateAisleScanRequestDto(
    int AisleId,
    int? ShelfLevelId,
    int RobotId,
    decimal? EmptyPercentage,
    string? ImageBase64,
    string? ImageUrl);

/// <summary>
/// Request mới (Phase 2): robot gửi ảnh chụp kèm theo AisleId/RobotId,
/// BE tự tính EmptyPercentage + upload ảnh lên Cloudinary + broadcast staff.
/// </summary>
public sealed record AisleScanWithPhotoRequestDto(
    int AisleId,
    int RobotId,
    string ImageBase64,
    /// <summary>Threshold % trống để đánh dấu NeedsRestock (mặc định 30).</summary>
    decimal? RestockThreshold = null);
