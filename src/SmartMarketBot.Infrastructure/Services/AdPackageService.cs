using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdPackageService(AppDbContext db, ILocalizationService localizer) : IAdPackageService
{
    public async Task<IReadOnlyList<AdPackageDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var packages = await db.AdPackages
            .AsNoTracking()
            .Include(p => p.AdCampaigns)
            .ToListAsync(cancellationToken);

        return packages.Select(p => new AdPackageDto(
            p.PackageId,
            p.PackageName,
            p.PricePackage,
            p.PriceRoute,
            p.BasePriceClick,
            p.AdScore,
            p.Status,
            p.AdCampaigns.Count(c => c.Status == CampaignStatus.Active)
        )).ToList();
    }

    public async Task<AdPackageDto?> GetByIdAsync(int packageId, CancellationToken cancellationToken = default)
    {
        var package = await db.AdPackages
            .AsNoTracking()
            .Include(p => p.AdCampaigns)
            .FirstOrDefaultAsync(p => p.PackageId == packageId, cancellationToken);

        if (package is null)
            return null;

        return new AdPackageDto(
            package.PackageId,
            package.PackageName,
            package.PricePackage,
            package.PriceRoute,
            package.BasePriceClick,
            package.AdScore,
            package.Status,
            package.AdCampaigns.Count(c => c.Status == CampaignStatus.Active)
        );
    }

    public async Task<AdPackageDto> CreateAsync(CreateAdPackageRequestDto request, CancellationToken cancellationToken = default)
    {
        var package = new AdPackage
        {
            PackageName = request.PackageName,
            PricePackage = request.PricePackage,
            PriceRoute = request.PriceRoute,
            BasePriceClick = request.BasePriceClick,
            AdScore = request.AdScore,
            Status = "Active"
        };

        db.AdPackages.Add(package);
        await db.SaveChangesAsync(cancellationToken);

        return new AdPackageDto(
            package.PackageId,
            package.PackageName,
            package.PricePackage,
            package.PriceRoute,
            package.BasePriceClick,
            package.AdScore,
            package.Status,
            0
        );
    }

    public async Task<AdPackageDto> UpdateAsync(int packageId, UpdateAdPackageRequestDto request, CancellationToken cancellationToken = default)
    {
        var package = await db.AdPackages
            .Include(p => p.AdCampaigns)
            .FirstOrDefaultAsync(p => p.PackageId == packageId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", packageId));

        package.PackageName = request.PackageName;
        package.PricePackage = request.PricePackage;
        package.PriceRoute = request.PriceRoute;
        package.BasePriceClick = request.BasePriceClick;
        package.AdScore = request.AdScore;
        package.Status = request.Status;

        await db.SaveChangesAsync(cancellationToken);

        return new AdPackageDto(
            package.PackageId,
            package.PackageName,
            package.PricePackage,
            package.PriceRoute,
            package.BasePriceClick,
            package.AdScore,
            package.Status,
            package.AdCampaigns.Count(c => c.Status == CampaignStatus.Active)
        );
    }

    public async Task<bool> DeleteAsync(int packageId, CancellationToken cancellationToken = default)
    {
        var package = await db.AdPackages
            .Include(p => p.AdCampaigns)
            .FirstOrDefaultAsync(p => p.PackageId == packageId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", packageId));

        if (package.AdCampaigns.Any())
            throw new InvalidOperationException(localizer.Get("AdPackageHasCampaigns"));

        db.AdPackages.Remove(package);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
