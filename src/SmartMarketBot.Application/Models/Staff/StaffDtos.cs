using System.ComponentModel.DataAnnotations;

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
    bool HasWarehouseStock,
    int AisleId,                // required by POST /complete
    int? AisleNodeId = null);   // optional, helps narrow the scan

public sealed record RestockTaskListResponseDto(
    int TotalPending,
    IReadOnlyList<RestockTaskDto> Tasks);

/// <summary>Nhân viên xác nhận hoàn tất bổ sung hàng.</summary>
public sealed record CompleteRestockRequestDto(
    [Range(1, int.MaxValue, ErrorMessage = "AisleId phải hợp lệ (>= 1).")]
    int AisleId,

    int? AisleNodeId = null,

    int? SlotId = null,

    int? QuantityAdded = null);

/// <summary>Request báo cáo kệ trống từ robot hoặc khách hàng.</summary>
public sealed record ReportOosRequestDto(
    [Range(1, int.MaxValue, ErrorMessage = "SlotId phải hợp lệ.")]
    int SlotId,

    [Range(1, int.MaxValue, ErrorMessage = "RobotId phải hợp lệ.")]
    int RobotId,

    string? ImageUrl,

    [Range(0.0, 100.0, ErrorMessage = "EmptyPercentage phải từ 0 đến 100.")]
    decimal EmptyPercentage,

    bool IsOccluded = false,
    string? OcclusionReason = null);

public sealed record ReportOosResponseDto(
    int ScanId,
    bool NeedsRestock,
    bool HasWarehouseStock,
    string Message);
