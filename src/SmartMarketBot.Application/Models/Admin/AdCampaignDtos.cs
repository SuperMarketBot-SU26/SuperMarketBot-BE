using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Admin;

public sealed record AdCampaignDto(
    int AdCampaignId,
    int PackageId,
    int BrandId,
    int? RobotZoneId,
    string CampaignName,
    DateTime StartDate,
    DateTime EndDate,
    string Status);

public sealed record CreateAdCampaignRequestDto(
    [Range(1, int.MaxValue)] int PackageId,
    [Range(1, int.MaxValue)] int BrandId,
    [Range(1, int.MaxValue)] int? RobotZoneId = null,
    [Required, MinLength(1), MaxLength(200)] string CampaignName = "",
    [Required] DateTime? StartDate = null,
    [Required] DateTime? EndDate = null,
    [MaxLength(50)] string Status = "Scheduled");

public sealed record UpdateAdCampaignRequestDto(
    [Range(1, int.MaxValue)] int? RobotZoneId = null,
    [Required, MinLength(1), MaxLength(200)] string CampaignName = "",
    [Required] DateTime? StartDate = null,
    [Required] DateTime? EndDate = null,
    [MaxLength(50)] string Status = "Scheduled");