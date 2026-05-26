namespace SmartMarketBot.Application.Models.Products;

public sealed record ProductDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    bool IsActive,
    string? Barcode,
    string? ImageUrl,
    int ProductTypeId);
