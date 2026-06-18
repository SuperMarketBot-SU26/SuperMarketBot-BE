using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdminBrandService(AppDbContext db, ILocalizationService localizer) : IAdminBrandService
{
    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Brands
            .AsNoTracking()
            .OrderBy(b => b.BrandName)
            .Select(b => new BrandDto(b.BrandId, b.BrandName, b.Wallet, b.Description))
            .ToListAsync(ct);
    }

    public async Task<BrandDto?> GetByIdAsync(int brandId, CancellationToken ct = default)
    {
        return await db.Brands
            .AsNoTracking()
            .Where(b => b.BrandId == brandId)
            .Select(b => new BrandDto(b.BrandId, b.BrandName, b.Wallet, b.Description))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BrandDto> CreateAsync(CreateBrandRequestDto request, CancellationToken ct = default)
    {
        var brand = new Domain.Entities.Brand
        {
            BrandName = request.BrandName.Trim(),
            Wallet = request.InitialWallet,
            Description = request.Description
        };
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return new BrandDto(brand.BrandId, brand.BrandName, brand.Wallet, brand.Description);
    }

    public async Task<BrandDto> UpdateAsync(int brandId, UpdateBrandRequestDto request, CancellationToken ct = default)
    {
        var brand = await db.Brands.FindAsync([brandId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", brandId));

        brand.BrandName = request.BrandName.Trim();
        if (request.Wallet is not null) brand.Wallet = request.Wallet.Value;
        brand.Description = request.Description;
        await db.SaveChangesAsync(ct);
        return new BrandDto(brand.BrandId, brand.BrandName, brand.Wallet, brand.Description);
    }

    public async Task DeleteAsync(int brandId, CancellationToken ct = default)
    {
        var brand = await db.Brands.FindAsync([brandId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", brandId));

        // Không xóa nếu Brand còn AdCampaign đang chạy (FK NoAction sẽ ném exception, nhưng kiểm tra trước để message rõ)
        var hasActiveCampaign = await db.AdCampaigns.AnyAsync(c => c.BrandId == brandId, ct);
        if (hasActiveCampaign)
            throw new InvalidOperationException(localizer.Get("BrandHasActiveCampaign"));

        db.Brands.Remove(brand);
        await db.SaveChangesAsync(ct);
    }
}