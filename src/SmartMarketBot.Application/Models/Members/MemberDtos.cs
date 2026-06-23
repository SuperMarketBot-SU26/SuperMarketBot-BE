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

// ─── Member Profile ────────────────────────────────────────────────────────────

/// <summary>Thông tin profile đầy đủ của member đang đăng nhập.</summary>
public sealed record MemberProfileDto(
    int MemberId,
    int AccountId,
    string FullName,
    string Email,
    string? Phone,
    string? FacePath,
    int TotalPoints,
    decimal? SpendingLimit,
    string MembershipTier,
    string AccountStatus,
    DateTime CreatedAt);

/// <summary>Request body cho PUT /api/members/me</summary>
public sealed record UpdateProfileRequestDto(
    [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2, ErrorMessage = "Tên phải từ 2–100 ký tự.")]
    string? FullName,

    [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [System.ComponentModel.DataAnnotations.StringLength(20)]
    string? Phone);

// ─── Budget ────────────────────────────────────────────────────────────────────

/// <summary>Thông tin ngân sách hiện tại của member.</summary>
public sealed record MemberBudgetDto(
    int MemberId,
    decimal? SpendingLimit,
    string Message);

/// <summary>Request body cho PUT /api/members/me/budget</summary>
public sealed record UpdateBudgetRequestDto(
    [System.ComponentModel.DataAnnotations.Range(0, 999_999_999, ErrorMessage = "Ngân sách phải >= 0.")]
    decimal? SpendingLimit);

// ─── Health Preferences (Chế độ ăn & Dị ứng) ──────────────────────────────────

/// <summary>Một health tag trong hệ thống (diet / allergen / ...).</summary>
public sealed record HealthTagDto(
    int HealthTagId,
    string TagName,
    /// <summary>'diet' | 'allergen' | 'ingredient' | ...</summary>
    string TagType);

/// <summary>Một preference của member với một health tag cụ thể.</summary>
public sealed record MemberHealthPreferenceItemDto(
    int HealthTagId,
    string TagName,
    string TagType,
    /// <summary>'Allergy' | 'Avoid' | 'Preferred'</summary>
    string Status);

/// <summary>Toàn bộ health preferences của member.</summary>
public sealed record MemberHealthPreferencesDto(
    int MemberId,
    IReadOnlyList<MemberHealthPreferenceItemDto> Allergies,
    IReadOnlyList<MemberHealthPreferenceItemDto> Avoids,
    IReadOnlyList<MemberHealthPreferenceItemDto> Preferreds);

/// <summary>Một item trong request upsert health preferences.</summary>
public sealed record HealthPreferenceItemDto(
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "HealthTagId không hợp lệ.")]
    int HealthTagId,

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression("^(Allergy|Avoid|Preferred)$",
        ErrorMessage = "Status phải là 'Allergy', 'Avoid' hoặc 'Preferred'.")]
    string Status);

/// <summary>
/// Request body cho PUT /api/members/me/health-preferences.
/// Danh sách này sẽ THAY THẾ HOÀN TOÀN preferences hiện tại của member.
/// Gửi danh sách rỗng [] để xóa hết.
/// </summary>
public sealed record UpdateHealthPreferencesRequestDto(
    IReadOnlyList<HealthPreferenceItemDto> Preferences);

