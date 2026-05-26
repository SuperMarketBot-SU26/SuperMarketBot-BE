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
}
