using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record AdRouteResponseDto(
    int AdRouteId,
    string RouteName,
    string? Description,
    bool IsActive,
    bool IsAutonomous,
    int? SemanticObjectId,
    string? SemanticObjectLabel,
    DateTime CreatedAt,
    List<AdRouteNodeDto> Nodes,
    List<int> CampaignIds);

public sealed record AdRouteNodeDto(
    int AdRouteNodeId,
    int NodeId,
    string? NodeName,
    int SequenceOrder,
    int DwellTimeSeconds,
    int? ZoneId,
    string? ZoneName);

public sealed record CreateAdRouteRequestDto
{
    [Required(ErrorMessage = "RouteName không được để trống.")]
    [MaxLength(100, ErrorMessage = "RouteName không được vượt quá 100 ký tự.")]
    public required string RouteName { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// If true: Autonomous mode (sequential playlist from stops)
    /// If false: Zone/Shelf mode (AABB spatial detection)
    /// </summary>
    public bool IsAutonomous { get; init; } = false;

    /// <summary>
    /// For Zone/Shelf mode: target SemanticObject to track.
    /// </summary>
    public int? SemanticObjectId { get; init; }

    [Required(ErrorMessage = "Danh sách NodeIds là bắt buộc và phải có ít nhất 1 node.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 node trong lộ trình.")]
    public required List<AdRouteNodeInput> Nodes { get; init; }

    public List<int>? CampaignIds { get; init; }
}

public sealed record AdRouteNodeInput
{
    [Required]
    public required int NodeId { get; init; }

    public int SequenceOrder { get; init; }

    public int DwellTimeSeconds { get; init; } = 30;

    /// <summary>
    /// ZoneId of the aisle this node belongs to.
    /// Used to group nodes into playlists for Autonomous mode.
    /// </summary>
    public int? ZoneId { get; init; }
}

public sealed record UpdateAdRouteRequestDto
{
    [Required(ErrorMessage = "RouteName không được để trống.")]
    [MaxLength(100, ErrorMessage = "RouteName không được vượt quá 100 ký tự.")]
    public required string RouteName { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;

    public bool IsAutonomous { get; init; } = false;

    public int? SemanticObjectId { get; init; }

    public List<AdRouteNodeInput>? Nodes { get; init; }

    public List<int>? CampaignIds { get; init; }
}
