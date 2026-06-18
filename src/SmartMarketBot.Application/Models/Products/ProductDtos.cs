namespace SmartMarketBot.Application.Models.Products;

public sealed record ProductDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    string Status,
    string? ImageUrl,
    int ProductTypeId);
