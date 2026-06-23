using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record LogInteractionRequestDto
{
    [Required(ErrorMessage = "AdCampaignId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "AdCampaignId không hợp lệ.")]
    public required int AdCampaignId { get; init; }

    [Required(ErrorMessage = "ActionType là bắt buộc.")]
    [RegularExpression("^(Click|Navigation|Impression)$", ErrorMessage = "ActionType chỉ nhận giá trị 'Click', 'Navigation', hoặc 'Impression'.")]
    public required string ActionType { get; init; }

    public int? SponsoredId { get; init; }
    public int? ProductId { get; init; }
    public int? RobotId { get; init; }
    public int? RobotZoneId { get; init; }
    public int? ZoneId { get; init; }
    public int? SlotId { get; init; }
    public int? MemberId { get; init; }

    [MaxLength(100, ErrorMessage = "SessionID không được vượt quá 100 ký tự.")]
    public string? SessionId { get; init; }

    public int? XCoord { get; init; }
    public int? YCoord { get; init; }
}

public sealed record LogInteractionResponseDto(
    bool Success,
    int LogId,
    decimal ChargedAmount,
    bool IsFraud,
    string? FraudReason,
    string Message);

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
    string? SessionId,
    bool IsFraud);
