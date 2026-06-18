using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Members;

// ─── Flow 3: Budget & Health ──────────────────────────────────────────────────

public sealed record SetBudgetRequestDto(
    [Range(0, 999_999_999, ErrorMessage = "Ngân sách phải >= 0.")]
    decimal Budget);

public sealed record SetBudgetResponseDto(
    int MemberId,
    decimal Budget,
    string SearchMode,
    string Message);

public sealed record ScanItemRequestDto(
    [Required(ErrorMessage = "ProductId không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    int ProductId,

    [Range(0, 999_999_999, ErrorMessage = "Tổng giỏ hàng hiện tại phải >= 0.")]
    decimal CurrentCartTotal,

    [Range(1, 999, ErrorMessage = "Số lượng quét phải >= 1.")]
    int Quantity = 1);

public sealed record AlternativeProductDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    string? ImageUrl,
    string Reason);

public sealed record ScanItemResponseDto(
    bool IsAllowed,
    /// <summary>null | 'Allergy' | 'BudgetExceeded' | 'DuplicatePurchase'</summary>
    string? AlertType,
    string? AlertMessage,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    decimal NewCartTotal,
    decimal? RemainingBudget,
    IReadOnlyList<AlternativeProductDto> AlternativeProducts);

// ─── Flow 2: Deal Hunter ───────────────────────────────────────────────────────

public sealed record MemberDealDto(
    int ProductId,
    string ProductName,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    decimal DiscountPct,
    string DealType,   // 'Promotion' | 'Sponsored' | 'Birthday' | 'Budget'
    string? Reason,
    string? ImageUrl);

public sealed record MemberDealsResponseDto(
    int MemberId,
    IReadOnlyList<MemberDealDto> Deals,
    int TotalDeals);

// ─── Member Profile & Alerts ──────────────────────────────────────────────────

public sealed record MemberAlertDto(
    int AlertId,
    string AlertType,
    string AlertMessage,
    DateTime CreatedAt,
    bool IsRead);

public sealed record MemberAlertsResponseDto(
    int MemberId,
    int UnreadCount,
    IReadOnlyList<MemberAlertDto> Alerts);

public sealed record MarkAlertsReadRequestDto(IReadOnlyList<int> AlertIds);

public sealed record MemberEventDto(
    int EventId,
    string EventName,
    DateOnly EventDate,
    decimal? DiscountPct,
    bool IsProcessed);
