namespace SmartMarketBot.Application.Models.Staff;

// ─── Flow 4: Out-of-Stock Handler ─────────────────────────────────────────────

/// <summary>Một nhiệm vụ bổ sung hàng giao cho nhân viên.</summary>
public sealed record RestockTaskDto(
    int ScanId,
    int SlotId,
    string SlotCode,
    string ShelfLocation,       // "Khu B - Dãy B2 - Tầng 2 - Ô 01"
    int ProductId,
    string ProductName,
    string? ProductImageUrl,
    int CurrentQuantity,
    decimal EmptyPercentage,
    DateTime ReportedAt,
    string Priority,            // 'High' | 'Medium' | 'Low'
    bool HasWarehouseStock);

public sealed record RestockTaskListResponseDto(
    int TotalPending,
    IReadOnlyList<RestockTaskDto> Tasks);

/// <summary>Nhân viên xác nhận hoàn tất bổ sung hàng.</summary>
public sealed record CompleteRestockRequestDto(
    int ScanId,
    int SlotId,
    int QuantityAdded);

/// <summary>Request báo cáo kệ trống từ robot hoặc khách hàng.</summary>
public sealed record ReportOosRequestDto(
    int SlotId,
    int RobotId,
    string? ImageUrl,
    decimal EmptyPercentage,
    bool IsOccluded = false,
    string? OcclusionReason = null);

public sealed record ReportOosResponseDto(
    int ScanId,
    bool NeedsRestock,
    bool HasWarehouseStock,
    string Message);
