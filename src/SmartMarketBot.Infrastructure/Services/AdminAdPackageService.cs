using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdminAdPackageService(AppDbContext db, ILocalizationService localizer) : IAdminAdPackageService
{
    public async Task<IReadOnlyList<AdPackageDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.AdPackages
            .AsNoTracking()
            .OrderBy(p => p.PackageName)
            .Select(p => new AdPackageDto(
                p.PackageId, p.PackageName, p.PricePackage, p.PriceRoute,
                p.BasePriceClick, p.AdScore, p.Status))
            .ToListAsync(ct);
    }

    public async Task<AdPackageDto?> GetByIdAsync(int packageId, CancellationToken ct = default)
    {
        return await db.AdPackages
            .AsNoTracking()
            .Where(p => p.PackageId == packageId)
            .Select(p => new AdPackageDto(
                p.PackageId, p.PackageName, p.PricePackage, p.PriceRoute,
                p.BasePriceClick, p.AdScore, p.Status))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<AdPackageDto> CreateAsync(CreateAdPackageRequestDto request, CancellationToken ct = default)
    {
        var pkg = new Domain.Entities.AdPackage
        {
            PackageName = request.PackageName.Trim(),
            PricePackage = request.PricePackage,
            PriceRoute = request.PriceRoute,
            BasePriceClick = request.BasePriceClick,
            AdScore = request.AdScore,
            Status = request.Status
        };
        db.AdPackages.Add(pkg);
        await db.SaveChangesAsync(ct);
        return new AdPackageDto(pkg.PackageId, pkg.PackageName, pkg.PricePackage,
            pkg.PriceRoute, pkg.BasePriceClick, pkg.AdScore, pkg.Status);
    }

    public async Task<AdPackageDto> UpdateAsync(int packageId, UpdateAdPackageRequestDto request, CancellationToken ct = default)
    {
        var pkg = await db.AdPackages.FindAsync([packageId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", packageId));

        pkg.PackageName = request.PackageName.Trim();
        pkg.PricePackage = request.PricePackage;
        pkg.PriceRoute = request.PriceRoute;
        pkg.BasePriceClick = request.BasePriceClick;
        pkg.AdScore = request.AdScore;
        pkg.Status = request.Status;
        await db.SaveChangesAsync(ct);
        return new AdPackageDto(pkg.PackageId, pkg.PackageName, pkg.PricePackage,
            pkg.PriceRoute, pkg.BasePriceClick, pkg.AdScore, pkg.Status);
    }

    public async Task DeleteAsync(int packageId, CancellationToken ct = default)
    {
        var pkg = await db.AdPackages.FindAsync([packageId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", packageId));

        var hasCampaign = await db.AdCampaigns.AnyAsync(c => c.PackageId == packageId, ct);
        if (hasCampaign)
            throw new InvalidOperationException(localizer.Get("AdPackageInUse"));

        db.AdPackages.Remove(pkg);
        await db.SaveChangesAsync(ct);
    }
}