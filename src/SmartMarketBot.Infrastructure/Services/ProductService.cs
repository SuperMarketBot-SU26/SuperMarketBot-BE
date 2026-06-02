using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.ProductName)
            .Select(x => new ProductDto(
                x.ProductID,
                x.ProductName,
                x.UnitPrice,
                x.IsActive,
                x.Barcode,
                x.ImageUrl,
                x.ProductTypeID))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductID == productId)
            .Select(x => new ProductDto(
                x.ProductID,
                x.ProductName,
                x.UnitPrice,
                x.IsActive,
                x.Barcode,
                x.ImageUrl,
                x.ProductTypeID))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAlternativeProductsAsync(
        int productId, int? memberId, CancellationToken cancellationToken = default)
    {
        // Lấy ProductTypeID của sản phẩm gốc
        var source = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductID == productId)
            .Select(x => new { x.ProductTypeID, x.UnitPrice })
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null) return [];

        // Lấy TagID dị ứng của member (nếu có)
        HashSet<int> allergyTagIds = [];
        if (memberId.HasValue)
        {
            allergyTagIds = (await dbContext.MemberHealthPreferences
                .AsNoTracking()
                .Where(mhp => mhp.MemberID == memberId.Value && mhp.IsAllergy)
                .Select(mhp => mhp.TagID)
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        // Lấy ProductID chứa thành phần dị ứng
        HashSet<int> allergenProductIds = [];
        if (allergyTagIds.Count > 0)
        {
            allergenProductIds = (await dbContext.ProductHealthTags
                .AsNoTracking()
                .Where(pht => allergyTagIds.Contains(pht.TagID))
                .Select(pht => pht.ProductID)
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        // Phân khúc giá ±50%
        var minPrice = source.UnitPrice * 0.5m;
        var maxPrice = source.UnitPrice * 1.5m;

        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductTypeID == source.ProductTypeID
                && x.ProductID != productId
                && x.IsActive
                && !allergenProductIds.Contains(x.ProductID)
                && x.UnitPrice >= minPrice
                && x.UnitPrice <= maxPrice)
            .OrderBy(x => x.UnitPrice)
            .Take(5)
            .Select(x => new ProductDto(
                x.ProductID,
                x.ProductName,
                x.UnitPrice,
                x.IsActive,
                x.Barcode,
                x.ImageUrl,
                x.ProductTypeID))
            .ToListAsync(cancellationToken);
    }
}
