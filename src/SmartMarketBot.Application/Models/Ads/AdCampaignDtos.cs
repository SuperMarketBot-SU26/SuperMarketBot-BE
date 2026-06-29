using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record CampaignResponseDto(
    int AdCampaignId,
    string CampaignName,
    int PackageId,
    string PackageName,
    int BrandId,
    string BrandName,
    int? SemanticObjectId,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int SponsoredProductCount,
    decimal TotalSpent);

public sealed record CreateCampaignRequestDto
{
    [Required(ErrorMessage = "PackageId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "PackageId không hợp lệ.")]
    public required int PackageId { get; init; }

    [Required(ErrorMessage = "BrandId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "BrandId không hợp lệ.")]
    public required int BrandId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "SemanticObjectId không hợp lệ.")]
    public int? SemanticObjectId { get; init; }

    [Required(ErrorMessage = "CampaignName không được để trống.")]
    [MaxLength(200, ErrorMessage = "CampaignName không được vượt quá 200 ký tự.")]
    public required string CampaignName { get; init; }

    [Required(ErrorMessage = "StartDate là bắt buộc.")]
    public required DateTime StartDate { get; init; }

    [Required(ErrorMessage = "EndDate là bắt buộc.")]
    public required DateTime EndDate { get; init; }

    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm trong chiến dịch.")]
    public List<int>? ProductIds { get; init; }
}

public sealed record UpdateCampaignRequestDto
{
    [Required(ErrorMessage = "CampaignName không được để trống.")]
    [MaxLength(200, ErrorMessage = "CampaignName không được vượt quá 200 ký tự.")]
    public required string CampaignName { get; init; }

    [Required(ErrorMessage = "StartDate là bắt buộc.")]
    public required DateTime StartDate { get; init; }

    [Required(ErrorMessage = "EndDate là bắt buộc.")]
    public required DateTime EndDate { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "SemanticObjectId không hợp lệ.")]
    public int? SemanticObjectId { get; init; }
}

public sealed record ActivateCampaignResponseDto(
    int AdCampaignId,
    string CampaignName,
    string PreviousStatus,
    string NewStatus,
    decimal AmountCharged,
    decimal RemainingWalletBalance);

public sealed record PauseCampaignResponseDto(
    int AdCampaignId,
    string CampaignName,
    string Reason,
    string NewStatus);

public sealed record CancelCampaignResponseDto(
    int AdCampaignId,
    string CampaignName,
    string NewStatus,
    decimal RefundedAmount);

public sealed record CampaignListRequestDto
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }
    public int? BrandId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? SearchTerm { get; init; }
}

public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
