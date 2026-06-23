using System.ComponentModel.DataAnnotations;
using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.Application.Models.SemanticObjects;

public sealed record AssignProductRequestDto
{
    [Required(ErrorMessage = "ProductId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    public required int ProductId { get; init; }
}

public sealed record SemanticObjectDto(
    int ObjectId,
    int MapId,
    string ObjectType,
    double XMin,
    double YMin,
    double XMax,
    double YMax,
    string? Label,
    double? Confidence,
    DateTime? DetectedAt,
    string? ImageUrl,
    int? ProductId,
    string? ProductName,
    decimal? UnitPrice,
    string? ProductImageUrl);

public sealed record SemanticObjectListResponseDto(
    IReadOnlyList<SemanticObjectDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
