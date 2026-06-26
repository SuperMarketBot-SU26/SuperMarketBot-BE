using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return await SearchProductsAsync(null, null, null, null, null, null, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(
        string? keyword,
        int? categoryId,
        int? subcategoryId,
        int? productTypeId,
        IReadOnlyList<int>? healthTagIds,
        bool? availableOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();
            query = query.Where(x => x.ProductName.Contains(normalized)
                || (x.Description != null && x.Description.Contains(normalized)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.ProductType != null
                && x.ProductType.Subcategory != null
                && x.ProductType.Subcategory.CategoryId == categoryId.Value);
        }

        if (subcategoryId.HasValue)
        {
            query = query.Where(x => x.ProductType != null
                && x.ProductType.Subcategory != null
                && x.ProductType.Subcategory.SubcategoryId == subcategoryId.Value);
        }

        if (productTypeId.HasValue)
        {
            query = query.Where(x => x.ProductTypeId == productTypeId.Value);
        }

        if (healthTagIds is { Count: > 0 })
        {
            var ids = healthTagIds.Distinct().ToList();
            query = query.Where(x => x.ProductHealthTags.Any(pht => ids.Contains(pht.HealthTagId)));
        }

        if (availableOnly == true)
        {
            query = query.Where(x => x.Status == "Available");
        }

        return await query
            .OrderBy(x => x.ProductName)
            .Select(x => new ProductDto(
                x.ProductId,
                x.ProductName,
                x.UnitPrice,
                x.Status,
                x.ImageUrl,
                x.ProductTypeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new ProductDto(
                x.ProductId,
                x.ProductName,
                x.UnitPrice,
                x.Status,
                x.ImageUrl,
                x.ProductTypeId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDetailDto?> GetProductDetailByIdAsync(int productId, int? memberId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.ProductHealthTags)
            .ThenInclude(x => x.HealthTag)
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (product is null) return null;

        var isFavorite = memberId.HasValue && await dbContext.MemberFavoriteProducts
            .AsNoTracking()
            .AnyAsync(x => x.MemberId == memberId.Value && x.ProductId == productId, cancellationToken);

        return new ProductDetailDto(
            product.ProductId,
            product.ProductName,
            product.UnitPrice,
            product.PromotionPrice,
            product.Status,
            product.ImageUrl,
            product.Description,
            product.ProductTypeId,
            product.PromotionPrice.HasValue && product.PromotionPrice.Value < product.UnitPrice,
            isFavorite,
            product.ProductHealthTags
                .Select(x => new HealthTagDto(x.HealthTag!.HealthTagId, x.HealthTag.TagName, x.HealthTag.TagType))
                .OrderBy(x => x.TagName)
                .ToList());
    }

    public async Task<IReadOnlyList<ProductDto>> GetAlternativeProductsAsync(
        int productId, int? memberId, CancellationToken cancellationToken = default)
    {
        // Lấy ProductTypeId của sản phẩm gốc
        var source = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new { x.ProductTypeId, x.UnitPrice })
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null) return [];

        // Lấy TagID dị ứng của member (nếu có)
        HashSet<int> allergyTagIds = [];
        if (memberId.HasValue)
        {
            allergyTagIds = (await dbContext.MemberHealthPreferences
                .AsNoTracking()
                .Where(mhp => mhp.MemberId == memberId.Value && mhp.Status == "Allergy")
                .Select(mhp => mhp.HealthTagId)
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        // Lấy ProductID chứa thành phần dị ứng
        HashSet<int> allergenProductIds = [];
        if (allergyTagIds.Count > 0)
        {
            allergenProductIds = (await dbContext.ProductHealthTags
                .AsNoTracking()
                .Where(pht => allergyTagIds.Contains(pht.HealthTagId))
                .Select(pht => pht.ProductId)
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        // Phân khúc giá ±50%
        var minPrice = source.UnitPrice * 0.5m;
        var maxPrice = source.UnitPrice * 1.5m;

        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductTypeId == source.ProductTypeId
                && x.ProductId != productId
                && x.Status == "Available"
                && !allergenProductIds.Contains(x.ProductId)
                && x.UnitPrice >= minPrice
                && x.UnitPrice <= maxPrice)
            .OrderBy(x => x.UnitPrice)
            .Take(5)
            .Select(x => new ProductDto(
                x.ProductId,
                x.ProductName,
                x.UnitPrice,
                x.Status,
                x.ImageUrl,
                x.ProductTypeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetUnmappedProductsAsync(CancellationToken cancellationToken = default)
    {
        // Lấy danh sách ProductID đã được gán vào SemanticObject
        var mappedProductIds = await dbContext.SemanticObjects
            .AsNoTracking()
            .Where(s => s.ProductId.HasValue)
            .Select(s => s.ProductId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await dbContext.Products
            .AsNoTracking()
            .Where(p => !mappedProductIds.Contains(p.ProductId) && p.Status == "Available")
            .OrderBy(p => p.ProductName)
            .Select(p => new ProductDto(
                p.ProductId,
                p.ProductName,
                p.UnitPrice,
                p.Status,
                p.ImageUrl,
                p.ProductTypeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.CategoryName)
            .Select(x => new CategoryDto(x.CategoryId, x.CategoryName, x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubcategoryDto>> GetSubcategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Subcategories
            .AsNoTracking()
            .OrderBy(x => x.SubcategoryName)
            .Select(x => new SubcategoryDto(x.SubcategoryId, x.CategoryId, x.SubcategoryName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductTypeDto>> GetProductTypesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductTypes
            .AsNoTracking()
            .OrderBy(x => x.TypeName)
            .Select(x => new ProductTypeDto(x.ProductTypeId, x.SubcategoryId, x.TypeName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HealthTagDto>> GetHealthTagsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.HealthTags
            .AsNoTracking()
            .OrderBy(x => x.TagName)
            .Select(x => new HealthTagDto(x.HealthTagId, x.TagName, x.TagType))
            .ToListAsync(cancellationToken);
    }
}
