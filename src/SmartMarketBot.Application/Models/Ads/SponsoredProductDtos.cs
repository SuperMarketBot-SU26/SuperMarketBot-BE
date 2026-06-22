using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record SponsoredProductDto(
    int SponsoredId,
    int AdCampaignId,
    string CampaignName,
    int ProductId,
    string ProductName,
    decimal ProductPrice,
    int Priority,
    string Status);

public sealed record AddSponsoredProductRequestDto(
    [Required(ErrorMessage = "AdCampaignId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "AdCampaignId không hợp lệ.")]
    int AdCampaignId,
    
    [Required(ErrorMessage = "ProductId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    int ProductId,
    
    [Required(ErrorMessage = "Priority là bắt buộc.")]
    [Range(0, 1000, ErrorMessage = "Priority phải từ 0 đến 1000.")]
    int Priority);

public sealed record UpdateSponsoredProductPriorityDto(
    [Required(ErrorMessage = "Priority là bắt buộc.")]
    [Range(0, 1000, ErrorMessage = "Priority phải từ 0 đến 1000.")]
    int Priority);

public sealed record BulkAddSponsoredProductRequestDto(
    [Required(ErrorMessage = "AdCampaignId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "AdCampaignId không hợp lệ.")]
    int AdCampaignId,
    
    [Required(ErrorMessage = "Products không được để trống.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm.")]
    IReadOnlyList<SponsoredProductItemDto> Products);

public sealed record SponsoredProductItemDto(
    [Required(ErrorMessage = "ProductId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    int ProductId,
    
    [Required(ErrorMessage = "Priority là bắt buộc.")]
    [Range(0, 1000, ErrorMessage = "Priority phải từ 0 đến 1000.")]
    int Priority);
