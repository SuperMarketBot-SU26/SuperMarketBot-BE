using System.ComponentModel.DataAnnotations;
using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.Application.Models.SemanticObjects;

public sealed record AssignProductTypeRequestDto
{
    [Required(ErrorMessage = "ProductTypeId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductTypeId không hợp lệ.")]
    public required int ProductTypeId { get; init; }
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
    int? ProductTypeId,
    string? ProductTypeName,
    string? SubcategoryName,
    string? CategoryName);

public sealed record SemanticObjectListResponseDto(
    IReadOnlyList<SemanticObjectDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
