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
    IReadOnlyList<HealthTagDto> HealthTags,
    string? AisleCode = null,
    int? LevelNumber = null,
    string? SlotCode = null);

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
    public List<int>? HealthTagIds { get; init; }
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
    public List<int>? HealthTagIds { get; init; }
}

public sealed record UpdateProductStatusRequestDto
{
    public required string Status { get; init; }
}

public sealed record ProductLocationDto(
    int SemanticObjectId,
    string? ShelfName,
    string? Zone);

public sealed record MobileProductSearchResultDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    string Status,
    string? ImageUrl,
    int ProductTypeId,
    ProductLocationDto? Location);
