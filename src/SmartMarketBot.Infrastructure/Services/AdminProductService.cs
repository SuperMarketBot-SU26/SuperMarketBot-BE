using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdminProductService(
    AppDbContext dbContext,
    ICloudStorageService cloudStorage,
    IFileStorageService fileStorage,
    ILocalizationService localizer,
    ILogger<AdminProductService> logger) : IAdminProductService
{
    private const string ProductImageFolder = "products";
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    public async Task<ProductDto> CreateProductAsync(
        CreateProductRequestDto request,
        Stream? imageStream,
        string? imageFileName,
        CancellationToken cancellationToken = default)
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

        // Sau khi SaveChanges để có ProductId thật → publicId chứa id, tránh đè ảnh cũ nếu retry.
        product.ImageUrl = await ResolveImageUrlAsync(product.ProductId, imageStream, imageFileName, request.ImageUrl, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(
        int productId,
        UpdateProductRequestDto request,
        Stream? imageStream,
        string? imageFileName,
        CancellationToken cancellationToken = default)
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
        if (request.Description is not null) product.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Status)) product.Status = request.Status;
        product.SubstituteProductId = request.SubstituteProductId;

        // File upload wins; otherwise honour request.ImageUrl string fallback.
        product.ImageUrl = await ResolveImageUrlAsync(productId, imageStream, imageFileName, request.ImageUrl, cancellationToken);

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

    private async Task<string?> ResolveImageUrlAsync(
        int productId,
        Stream? imageStream,
        string? imageFileName,
        string? fallbackUrl,
        CancellationToken cancellationToken)
    {
        if (imageStream is not null && !string.IsNullOrWhiteSpace(imageFileName))
        {
            return await UploadProductImageAsync(productId, imageStream, imageFileName, cancellationToken);
        }
        return fallbackUrl;
    }

    private async Task<string> UploadProductImageAsync(int productId, Stream stream, string fileName, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(localizer.Get("ImageOnlyAllowed"));
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        try
        {
            var publicId = $"product_{productId}_{Path.GetFileNameWithoutExtension(fileName)}";
            var url = await cloudStorage.UploadImageAsync(bytes, ProductImageFolder, publicId, cancellationToken);
            logger.LogInformation("[AdminProduct] Image uploaded to Cloudinary: ProductId={ProductId}, URL={Url}", productId, url);
            return url;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AdminProduct] Cloudinary upload failed, fallback to local: {Msg}", ex.Message);
            return await fileStorage.SaveAsync(new MemoryStream(bytes), fileName, ProductImageFolder, cancellationToken);
        }
    }

    private static ProductDto ToDto(Product product) => new(
        product.ProductId,
        product.ProductName,
        product.UnitPrice,
        product.Status,
        product.ImageUrl,
        product.ProductTypeId);
}
