using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record CreateCampaignWithProductsRequestDto
{
    [Required(ErrorMessage = "PackageId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "PackageId không hợp lệ.")]
    public required int PackageId { get; init; }

    [Required(ErrorMessage = "BrandId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "BrandId không hợp lệ.")]
    public required int BrandId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "SemanticObjectId không hợp lệ.")]
    public int? SemanticObjectId { get; init; }

    public List<int>? ZoneIds { get; init; }

    public List<int>? RouteIds { get; init; }

    [Required(ErrorMessage = "CampaignName không được để trống.")]
    [MaxLength(200, ErrorMessage = "CampaignName không được vượt quá 200 ký tự.")]
    public required string CampaignName { get; init; }

    [Required(ErrorMessage = "StartDate là bắt buộc.")]
    public required DateTime StartDate { get; init; }

    [Required(ErrorMessage = "EndDate là bắt buộc.")]
    public required DateTime EndDate { get; init; }

    [Required(ErrorMessage = "Danh sách ProductIDs là bắt buộc và phải có ít nhất 1 sản phẩm.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm trong chiến dịch.")]
    public required List<int> ProductIds { get; init; }
}

public sealed record SessionBindRequestDto
{
    [Required(ErrorMessage = "SessionID là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "SessionID không được vượt quá 100 ký tự.")]
    public required string SessionId { get; init; }

    [Required(ErrorMessage = "MemberID là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "MemberID không hợp lệ.")]
    public required int MemberId { get; init; }
}

public sealed record SessionBindResponseDto(
    int MatchedLogCount,
    int MemberId,
    string SessionId,
    string Message);

public sealed record AdResourceDto(
    int ResourceId,
    int AdCampaignId,
    string ResourceType,
    string ResourceUrl,
    string? ContentText,
    string? Resolution,
    string Status);

public sealed record CreateAdResourceRequestDto
{
    [Required(ErrorMessage = "AdCampaignId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "AdCampaignId không hợp lệ.")]
    public required int AdCampaignId { get; init; }

    [Required(ErrorMessage = "ResourceType là bắt buộc.")]
    [RegularExpression("^(IMAGE|VIDEO|VOICE_TEXT)$", ErrorMessage = "ResourceType phải là IMAGE, VIDEO, hoặc VOICE_TEXT.")]
    public required string ResourceType { get; init; }

    [MaxLength(500, ErrorMessage = "ResourceURL không được vượt quá 500 ký tự.")]
    public string? ResourceUrl { get; init; }

    public string? ContentText { get; init; }

    [MaxLength(20, ErrorMessage = "Resolution không được vượt quá 20 ký tự.")]
    public string? Resolution { get; init; }
}

public sealed record MediaContentDto(
    string ResourceType,
    string ResourceUrl,
    string? ContentText,
    string? Resolution);

public sealed record RobotPlaylistItemDto
{
    public int SponsoredId { get; init; }
    public int AdCampaignId { get; init; }
    public string CampaignName { get; init; } = string.Empty;
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal? ProductPrice { get; init; }
    public int Priority { get; init; }
    public int AdScore { get; init; }
    public DateTime EndDate { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public int DisplayDurationSeconds { get; init; }
    public List<MediaContentDto>? MediaContents { get; init; }
}

public sealed record RobotPlaylistResponseDto(
    int RobotId,
    int? CurrentZoneId,
    List<RobotPlaylistItemDto> Playlist,
    DateTime GeneratedAt,
    int? SemanticObjectId);
