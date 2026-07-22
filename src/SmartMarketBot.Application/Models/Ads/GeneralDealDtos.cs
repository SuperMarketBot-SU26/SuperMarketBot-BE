namespace SmartMarketBot.Application.Models.Ads;

/// <summary>
/// API General Deals — dành cho Guest (chưa đăng nhập) và Member đã đăng nhập.
/// Trả về tất cả sản phẩm đang giảm giá trên toàn siêu thị.
/// Nguồn 1: Product.PromotionPrice != null (deal thường / flash sale).
/// Nguồn 2: SponsoredProduct thuộc AdCampaign Active (quảng cáo khuyến mãi).
/// </summary>
public sealed record GeneralDealDto(
    int ProductId,
    string ProductName,
    decimal OriginalPrice,
    decimal? DealPrice,
    decimal? DiscountPercent,
    string? PromotionLabel,
    string? ImageUrl,
    string? ProductTypeName,
    int ProductTypeId,
    string? BrandName,
    int? BrandId,
    bool IsSystemBrand,
    IReadOnlyList<string> HealthTags,
    bool HasAllergenConflict,
    IReadOnlyList<string> AllergenConflicts,
    string? AdCampaignName,
    int? AdCampaignId,
    string? SlotCode,
    int? SlotId);

public sealed record GeneralDealsResponseDto(
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    IReadOnlyList<GeneralDealDto> Items);

public sealed record GeneralDealsFilterDto(
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int? ProductTypeId = null,
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int? CategoryId = null,
    [System.ComponentModel.DataAnnotations.Range(0, 100)] int? MinDiscountPercent = null,
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int PageNumber = 1,
    [System.ComponentModel.DataAnnotations.Range(1, 100)] int PageSize = 20);
