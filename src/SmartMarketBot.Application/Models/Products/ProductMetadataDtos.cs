namespace SmartMarketBot.Application.Models.Products;

public sealed record CategoryDto(
    int CategoryId,
    string CategoryName,
    string? Description);

public sealed record SubcategoryDto(
    int SubcategoryId,
    int CategoryId,
    string SubcategoryName);

public sealed record ProductTypeDto(
    int ProductTypeId,
    int SubcategoryId,
    string TypeName);

public sealed record HealthTagDto(
    int HealthTagId,
    string TagName,
    string TagType);
