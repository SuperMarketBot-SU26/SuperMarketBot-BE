using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

/// <summary>Robot báo "vừa tới Slot X tọa độ (x,y)" — BE tìm Zone chứa Slot và ghi impression cho mọi Sponsored thuộc AdCampaign active trong Zone đó.</summary>
public sealed record RouteImpressionRequestDto(
    [Required, Range(1, int.MaxValue)] int SlotId,
    [Required, Range(0, 100000)] int XCoord,
    [Required, Range(0, 100000)] int YCoord,
    [Range(1, int.MaxValue)] int? MemberId = null);
