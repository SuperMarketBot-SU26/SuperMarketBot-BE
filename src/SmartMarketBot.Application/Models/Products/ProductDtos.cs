namespace SmartMarketBot.Application.Models.Products;

public sealed record ProductDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    string Status,
    string? ImageUrl,
    int ProductTypeId);

public sealed record ProductDetailDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    decimal? PromotionPrice,
    string Status,
    string? ImageUrl,
    string? Description,
    int ProductTypeId,
    bool IsOnSale,
    bool IsFavorite,
    IReadOnlyList<HealthTagDto> HealthTags);

public sealed record CreateProductRequestDto
{
    public required int ProductTypeId { get; init; }
    public required string ProductName { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal? PromotionPrice { get; init; }
    public string? ImageUrl { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "Available";
    public int? SubstituteProductId { get; init; }
}

public sealed record UpdateProductRequestDto
{
    public int? ProductTypeId { get; init; }
    public string? ProductName { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? PromotionPrice { get; init; }
    public string? ImageUrl { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public int? SubstituteProductId { get; init; }
}

public sealed record UpdateProductStatusRequestDto
{
    public required string Status { get; init; }
}
