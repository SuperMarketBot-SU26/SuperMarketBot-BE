using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Admin;

public sealed record SponsoredProductDto(
    int SponsoredId,
    int AdCampaignId,
    int ProductId,
    int Priority,
    string Status);

public sealed record CreateSponsoredProductRequestDto(
    [Range(1, int.MaxValue)] int AdCampaignId,
    [Range(1, int.MaxValue)] int ProductId,
    [Range(0, 1000)] int Priority = 0,
    [MaxLength(50)] string Status = "Active");

public sealed record UpdateSponsoredProductRequestDto(
    [Range(0, 1000)] int Priority,
    [MaxLength(50)] string Status);