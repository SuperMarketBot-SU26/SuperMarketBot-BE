using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdminProductService(AppDbContext dbContext) : IAdminProductService
{
    public async Task<ProductDto> CreateProductAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var productTypeExists = await dbContext.ProductTypes
            .AsNoTracking()
            .AnyAsync(x => x.ProductTypeId == request.ProductTypeId, cancellationToken);

        if (!productTypeExists)
        {
            throw new KeyNotFoundException($"ProductType '{request.ProductTypeId}' không tồn tại.");
        }

        var product = new Product
        {
            ProductTypeId = request.ProductTypeId,
            ProductName = request.ProductName,
            UnitPrice = request.UnitPrice,
            PromotionPrice = request.PromotionPrice,
            ImageUrl = request.ImageUrl,
            Description = request.Description,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Available" : request.Status,
            SubstituteProductId = request.SubstituteProductId
        };

        if (request.HealthTagIds is { Count: > 0 })
        {
            var validTagIds = await dbContext.HealthTags
                .AsNoTracking()
                .Where(t => request.HealthTagIds.Contains(t.HealthTagId))
                .Select(t => t.HealthTagId)
                .ToListAsync(cancellationToken);

            foreach (var tagId in validTagIds)
            {
                product.ProductHealthTags.Add(new ProductHealthTag
                {
                    HealthTagId = tagId
                });
            }
        }

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(int productId, UpdateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FindAsync(new object[] { productId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{productId}' không tồn tại.");

        if (request.ProductTypeId.HasValue)
        {
            var exists = await dbContext.ProductTypes
                .AsNoTracking()
                .AnyAsync(x => x.ProductTypeId == request.ProductTypeId.Value, cancellationToken);
            if (!exists)
            {
                throw new KeyNotFoundException($"ProductType '{request.ProductTypeId.Value}' không tồn tại.");
            }

            product.ProductTypeId = request.ProductTypeId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.ProductName)) product.ProductName = request.ProductName;
        if (request.UnitPrice.HasValue) product.UnitPrice = request.UnitPrice.Value;
        if (request.PromotionPrice.HasValue) product.PromotionPrice = request.PromotionPrice.Value;
        if (request.ImageUrl is not null) product.ImageUrl = request.ImageUrl;
        if (request.Description is not null) product.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Status)) product.Status = request.Status;
        product.SubstituteProductId = request.SubstituteProductId;

        if (request.HealthTagIds is not null)
        {
            var existingTags = await dbContext.ProductHealthTags
                .Where(pht => pht.ProductId == productId)
                .ToListAsync(cancellationToken);
            dbContext.ProductHealthTags.RemoveRange(existingTags);

            if (request.HealthTagIds.Count > 0)
            {
                var validTagIds = await dbContext.HealthTags
                    .AsNoTracking()
                    .Where(t => request.HealthTagIds.Contains(t.HealthTagId))
                    .Select(t => t.HealthTagId)
                    .ToListAsync(cancellationToken);

                foreach (var tagId in validTagIds)
                {
                    dbContext.ProductHealthTags.Add(new ProductHealthTag
                    {
                        ProductId = productId,
                        HealthTagId = tagId
                    });
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task<ProductDto?> UpdateProductStatusAsync(int productId, UpdateProductStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FindAsync(new object[] { productId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{productId}' không tồn tại.");

        product.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product is null) return false;

        product.Status = "Inactive";
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductDto ToDto(Product product) => new(
        product.ProductId,
        product.ProductName,
        product.UnitPrice,
        product.Status,
        product.ImageUrl,
        product.ProductTypeId);
}
