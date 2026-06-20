using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record CampaignResponseDto(
    int AdCampaignId,
    string CampaignName,
    int PackageId,
    string PackageName,
    int BrandId,
    string BrandName,
    int? RobotZoneId,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int SponsoredProductCount,
    decimal TotalSpent);

public sealed record CreateCampaignRequestDto(
    [Required(ErrorMessage = "PackageId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "PackageId không hợp lệ.")]
    int PackageId,
    
    [Required(ErrorMessage = "BrandId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "BrandId không hợp lệ.")]
    int BrandId,
    
    [Range(1, int.MaxValue, ErrorMessage = "RobotZoneId không hợp lệ.")]
    int? RobotZoneId,
    
    [Required(ErrorMessage = "CampaignName không được để trống.")]
    [MaxLength(200, ErrorMessage = "CampaignName không được vượt quá 200 ký tự.")]
    string CampaignName,
    
    [Required(ErrorMessage = "StartDate là bắt buộc.")]
    DateTime StartDate,
    
    [Required(ErrorMessage = "EndDate là bắt buộc.")]
    DateTime EndDate);

public sealed record UpdateCampaignRequestDto(
    [Required(ErrorMessage = "CampaignName không được để trống.")]
    [MaxLength(200, ErrorMessage = "CampaignName không được vượt quá 200 ký tự.")]
    string CampaignName,
    
    [Required(ErrorMessage = "StartDate là bắt buộc.")]
    DateTime StartDate,
    
    [Required(ErrorMessage = "EndDate là bắt buộc.")]
    DateTime EndDate,
    
    [Range(1, int.MaxValue, ErrorMessage = "RobotZoneId không hợp lệ.")]
    int? RobotZoneId);

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
