using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class BrandService(AppDbContext db, ILocalizationService localizer) : IBrandService
{
    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var brands = await db.Brands
            .AsNoTracking()
            .Include(b => b.AdCampaigns)
            .ToListAsync(cancellationToken);
        
        return brands.Select(b => new BrandDto(
            b.BrandId,
            b.BrandName,
            b.Wallet,
            b.Description,
            b.AdCampaigns.Count(c => c.Status == CampaignStatus.Active)
        )).ToList();
    }

    public async Task<BrandDto?> GetByIdAsync(int brandId, CancellationToken cancellationToken = default)
    {
        var brand = await db.Brands
            .AsNoTracking()
            .Include(b => b.AdCampaigns)
            .FirstOrDefaultAsync(b => b.BrandId == brandId, cancellationToken);
        
        if (brand is null)
            return null;

        return new BrandDto(
            brand.BrandId,
            brand.BrandName,
            brand.Wallet,
            brand.Description,
            brand.AdCampaigns.Count(c => c.Status == CampaignStatus.Active)
        );
    }

    public async Task<BrandDto> CreateAsync(CreateBrandRequestDto request, CancellationToken cancellationToken = default)
    {
        var brand = new Brand
        {
            BrandName = request.BrandName,
            Description = request.Description,
            Wallet = 0
        };

        db.Brands.Add(brand);
        await db.SaveChangesAsync(cancellationToken);

        return new BrandDto(
            brand.BrandId,
            brand.BrandName,
            brand.Wallet,
            brand.Description,
            0
        );
    }

    public async Task<BrandDto> UpdateAsync(int brandId, UpdateBrandRequestDto request, CancellationToken cancellationToken = default)
    {
        var brand = await db.Brands
            .Include(b => b.AdCampaigns)
            .FirstOrDefaultAsync(b => b.BrandId == brandId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", brandId));

        brand.BrandName = request.BrandName;
        brand.Description = request.Description;

        await db.SaveChangesAsync(cancellationToken);

        return new BrandDto(
            brand.BrandId,
            brand.BrandName,
            brand.Wallet,
            brand.Description,
            brand.AdCampaigns.Count(c => c.Status == CampaignStatus.Active)
        );
    }

    public async Task<bool> DeleteAsync(int brandId, CancellationToken cancellationToken = default)
    {
        var brand = await db.Brands.FindAsync([brandId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", brandId));

        if (brand.AdCampaigns.Any(c => c.Status == CampaignStatus.Active))
            throw new InvalidOperationException(localizer.Get("BrandHasActiveCampaigns"));

        db.Brands.Remove(brand);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TopUpWalletResponseDto> TopUpWalletAsync(int brandId, TopUpWalletRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException(localizer.Get("AmountMustBePositive"));

        var brand = await db.Brands.FindAsync([brandId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", brandId));

        var previousBalance = brand.Wallet;
        brand.Wallet += request.Amount;

        await db.SaveChangesAsync(cancellationToken);

        return new TopUpWalletResponseDto(
            brand.BrandId,
            previousBalance,
            request.Amount,
            brand.Wallet
        );
    }
}
