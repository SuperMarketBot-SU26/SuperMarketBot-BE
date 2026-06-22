using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class SponsoredProductService(AppDbContext db, ILocalizationService localizer) : ISponsoredProductService
{
    public async Task<IReadOnlyList<SponsoredProductDto>> GetByCampaignIdAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var products = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.AdCampaign)
            .Include(sp => sp.Product)
            .Where(sp => sp.AdCampaignId == campaignId)
            .ToListAsync(cancellationToken);

        return products.Select(MapToDto).ToList();
    }

    public async Task<SponsoredProductDto?> GetByIdAsync(int sponsoredId, CancellationToken cancellationToken = default)
    {
        var product = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.AdCampaign)
            .Include(sp => sp.Product)
            .FirstOrDefaultAsync(sp => sp.SponsoredId == sponsoredId, cancellationToken);

        return product is null ? null : MapToDto(product);
    }

    public async Task<SponsoredProductDto> CreateAsync(AddSponsoredProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var campaignExists = await db.AdCampaigns.AnyAsync(c => c.AdCampaignId == request.AdCampaignId, cancellationToken);
        if (!campaignExists)
            throw new KeyNotFoundException(localizer.Get("CampaignNotFound", request.AdCampaignId));

        var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId, cancellationToken);
        if (!productExists)
            throw new KeyNotFoundException(localizer.Get("ProductNotFound", request.ProductId));

        var existing = await db.SponsoredProducts
            .AnyAsync(sp => sp.AdCampaignId == request.AdCampaignId && sp.ProductId == request.ProductId, cancellationToken);
        if (existing)
            throw new InvalidOperationException(localizer.Get("SponsoredProductExists"));

        var sponsoredProduct = new SponsoredProduct
        {
            AdCampaignId = request.AdCampaignId,
            ProductId = request.ProductId,
            Priority = request.Priority,
            Status = SponsoredProductStatus.Active
        };

        db.SponsoredProducts.Add(sponsoredProduct);
        await db.SaveChangesAsync(cancellationToken);

        var result = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.AdCampaign)
            .Include(sp => sp.Product)
            .FirstAsync(sp => sp.SponsoredId == sponsoredProduct.SponsoredId, cancellationToken);

        return MapToDto(result);
    }

    public async Task<IReadOnlyList<SponsoredProductDto>> BulkCreateAsync(BulkAddSponsoredProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AdCampaignId == request.AdCampaignId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", request.AdCampaignId));

        if (campaign.Status != CampaignStatus.Inactive)
            throw new InvalidOperationException(localizer.Get("CampaignNotInactive"));

        var productIds = request.Products.Select(p => p.ProductId).ToList();
        var existingProducts = await db.Products
            .Where(p => productIds.Contains(p.ProductId))
            .Select(p => p.ProductId)
            .ToListAsync(cancellationToken);

        var missingIds = productIds.Except(existingProducts).ToList();
        if (missingIds.Any())
            throw new KeyNotFoundException(localizer.Get("ProductsNotFound", string.Join(", ", missingIds)));

        var existingSponsored = await db.SponsoredProducts
            .Where(sp => sp.AdCampaignId == request.AdCampaignId)
            .Select(sp => sp.ProductId)
            .ToListAsync(cancellationToken);

        var newProducts = request.Products
            .Where(p => !existingSponsored.Contains(p.ProductId))
            .Select(p => new SponsoredProduct
            {
                AdCampaignId = request.AdCampaignId,
                ProductId = p.ProductId,
                Priority = p.Priority,
                Status = SponsoredProductStatus.Active
            })
            .ToList();

        if (newProducts.Any())
        {
            db.SponsoredProducts.AddRange(newProducts);
            await db.SaveChangesAsync(cancellationToken);
        }

        var results = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.AdCampaign)
            .Include(sp => sp.Product)
            .Where(sp => sp.AdCampaignId == request.AdCampaignId)
            .ToListAsync(cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    public async Task<SponsoredProductDto> UpdatePriorityAsync(int sponsoredId, UpdateSponsoredProductPriorityDto request, CancellationToken cancellationToken = default)
    {
        var product = await db.SponsoredProducts
            .Include(sp => sp.AdCampaign)
            .Include(sp => sp.Product)
            .FirstOrDefaultAsync(sp => sp.SponsoredId == sponsoredId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("SponsoredProductNotFound", sponsoredId));

        product.Priority = request.Priority;
        await db.SaveChangesAsync(cancellationToken);

        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(int sponsoredId, CancellationToken cancellationToken = default)
    {
        var product = await db.SponsoredProducts.FindAsync([sponsoredId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("SponsoredProductNotFound", sponsoredId));

        db.SponsoredProducts.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int sponsoredId, string status, CancellationToken cancellationToken = default)
    {
        var product = await db.SponsoredProducts
            .Include(sp => sp.AdCampaign)
            .Include(sp => sp.Product)
            .FirstOrDefaultAsync(sp => sp.SponsoredId == sponsoredId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("SponsoredProductNotFound", sponsoredId));

        product.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static SponsoredProductDto MapToDto(SponsoredProduct sp)
    {
        return new SponsoredProductDto(
            sp.SponsoredId,
            sp.AdCampaignId,
            sp.AdCampaign?.CampaignName ?? string.Empty,
            sp.ProductId,
            sp.Product?.ProductName ?? string.Empty,
            sp.Product?.UnitPrice ?? 0,
            sp.Priority,
            sp.Status
        );
    }
}
