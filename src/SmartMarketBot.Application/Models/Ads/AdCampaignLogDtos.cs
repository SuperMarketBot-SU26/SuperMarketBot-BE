using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record LogInteractionRequestDto(
    [Required(ErrorMessage = "AdCampaignId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "AdCampaignId không hợp lệ.")]
    int AdCampaignId,
    
    [Required(ErrorMessage = "ActionType là bắt buộc.")]
    [RegularExpression("^(Click|Navigation|Impression)$", ErrorMessage = "ActionType chỉ nhận giá trị 'Click', 'Navigation', hoặc 'Impression'.")]
    string ActionType,
    
    int? SponsoredId,
    
    int? ProductId,
    
    int? RobotId,
    
    int? RobotZoneId,
    
    int? ZoneId,
    
    int? SlotId,
    
    int? MemberId,
    
    [MaxLength(100, ErrorMessage = "SessionId không được vượt quá 100 ký tự.")]
    string? SessionId,
    
    decimal? XCoord,
    
    decimal? YCoord);

public sealed record LogInteractionResponseDto(
    bool Success,
    int LogId,
    decimal ChargedAmount,
    bool IsFraud,
    string? FraudReason,
    string Message);

public sealed record RobotPlaylistItemDto(
    int SponsoredId,
    int AdCampaignId,
    string CampaignName,
    int ProductId,
    string ProductName,
    decimal? ProductPrice,
    int Priority,
    int AdScore,
    DateTime EndDate,
    string MediaUrl,
    string MediaType,
    int DisplayDurationSeconds);

public sealed record RobotPlaylistResponseDto(
    int RobotId,
    int? CurrentZoneId,
    IReadOnlyList<RobotPlaylistItemDto> Playlist,
    DateTime GeneratedAt);

public sealed record AdCampaignLogDto(
    int LogId,
    int AdCampaignId,
    string? CampaignName,
    string ActionType,
    decimal ChargedAmount,
    DateTime Timestamp,
    int? SponsoredId,
    int? ProductId,
    string? ProductName,
    int? RobotId,
    int? ZoneId,
    int? MemberId,
    bool IsFraud);
