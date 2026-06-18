using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdminSponsoredProductService(AppDbContext db, ILocalizationService localizer) : IAdminSponsoredProductService
{
    public async Task<IReadOnlyList<SponsoredProductDto>> GetAllAsync(int? campaignId, int? productId, CancellationToken ct = default)
    {
        var query = db.SponsoredProducts.AsNoTracking();
        if (campaignId is not null) query = query.Where(s => s.AdCampaignId == campaignId);
        if (productId is not null) query = query.Where(s => s.ProductId == productId);

        return await query
            .OrderBy(s => s.AdCampaignId).ThenBy(s => s.Priority)
            .Select(s => new SponsoredProductDto(
                s.SponsoredId, s.AdCampaignId, s.ProductId, s.Priority, s.Status))
            .ToListAsync(ct);
    }

    public async Task<SponsoredProductDto?> GetByIdAsync(int sponsoredId, CancellationToken ct = default)
    {
        return await db.SponsoredProducts
            .AsNoTracking()
            .Where(s => s.SponsoredId == sponsoredId)
            .Select(s => new SponsoredProductDto(
                s.SponsoredId, s.AdCampaignId, s.ProductId, s.Priority, s.Status))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SponsoredProductDto> CreateAsync(CreateSponsoredProductRequestDto request, CancellationToken ct = default)
    {
        // Validate FK
        if (!await db.AdCampaigns.AnyAsync(c => c.AdCampaignId == request.AdCampaignId, ct))
            throw new KeyNotFoundException(localizer.Get("AdCampaignNotFound", request.AdCampaignId));
        if (!await db.Products.AnyAsync(p => p.ProductId == request.ProductId, ct))
            throw new KeyNotFoundException(localizer.Get("ProductNotFoundById", request.ProductId));

        // Tránh duplicate mapping
        var exists = await db.SponsoredProducts
            .AnyAsync(s => s.AdCampaignId == request.AdCampaignId && s.ProductId == request.ProductId, ct);
        if (exists)
            throw new InvalidOperationException(localizer.Get("SponsoredMappingExists"));

        var sp = new Domain.Entities.SponsoredProduct
        {
            AdCampaignId = request.AdCampaignId,
            ProductId = request.ProductId,
            Priority = request.Priority,
            Status = request.Status
        };
        db.SponsoredProducts.Add(sp);
        await db.SaveChangesAsync(ct);
        return new SponsoredProductDto(sp.SponsoredId, sp.AdCampaignId, sp.ProductId, sp.Priority, sp.Status);
    }

    public async Task<SponsoredProductDto> UpdateAsync(int sponsoredId, UpdateSponsoredProductRequestDto request, CancellationToken ct = default)
    {
        var sp = await db.SponsoredProducts.FindAsync([sponsoredId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("SponsoredProductNotFound", sponsoredId));

        sp.Priority = request.Priority;
        sp.Status = request.Status;
        await db.SaveChangesAsync(ct);
        return new SponsoredProductDto(sp.SponsoredId, sp.AdCampaignId, sp.ProductId, sp.Priority, sp.Status);
    }

    public async Task DeleteAsync(int sponsoredId, CancellationToken ct = default)
    {
        var sp = await db.SponsoredProducts.FindAsync([sponsoredId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("SponsoredProductNotFound", sponsoredId));

        db.SponsoredProducts.Remove(sp);
        await db.SaveChangesAsync(ct);
    }
}