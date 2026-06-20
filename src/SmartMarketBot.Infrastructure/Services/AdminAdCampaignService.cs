using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdminAdCampaignService(AppDbContext db, ILocalizationService localizer) : IAdminAdCampaignService
{
    public async Task<IReadOnlyList<AdCampaignDto>> GetAllAsync(int? brandId, CancellationToken ct = default)
    {
        var query = db.AdCampaigns.AsNoTracking();
        if (brandId is not null) query = query.Where(c => c.BrandId == brandId);

        return await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new AdCampaignDto(
                c.AdCampaignId, c.PackageId, c.BrandId, c.RobotZoneId,
                c.CampaignName, c.StartDate, c.EndDate, c.Status))
            .ToListAsync(ct);
    }

    public async Task<AdCampaignDto?> GetByIdAsync(int campaignId, CancellationToken ct = default)
    {
        return await db.AdCampaigns
            .AsNoTracking()
            .Where(c => c.AdCampaignId == campaignId)
            .Select(c => new AdCampaignDto(
                c.AdCampaignId, c.PackageId, c.BrandId, c.RobotZoneId,
                c.CampaignName, c.StartDate, c.EndDate, c.Status))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<AdCampaignDto> CreateAsync(CreateAdCampaignRequestDto request, CancellationToken ct = default)
    {
        var startDate = request.StartDate.GetValueOrDefault();
        var endDate = request.EndDate.GetValueOrDefault();

        if (endDate <= startDate)
            throw new ArgumentException(localizer.Get("CampaignDateInvalid"));

        // Validate FK exists
        if (!await db.AdPackages.AnyAsync(p => p.PackageId == request.PackageId, ct))
            throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", request.PackageId));
        if (!await db.Brands.AnyAsync(b => b.BrandId == request.BrandId, ct))
            throw new KeyNotFoundException(localizer.Get("BrandNotFound", request.BrandId));

        var campaign = new Domain.Entities.AdCampaign
        {
            PackageId = request.PackageId,
            BrandId = request.BrandId,
            RobotZoneId = request.RobotZoneId,
            CampaignName = request.CampaignName.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = request.Status
        };
        db.AdCampaigns.Add(campaign);
        await db.SaveChangesAsync(ct);
        return new AdCampaignDto(campaign.AdCampaignId, campaign.PackageId, campaign.BrandId,
            campaign.RobotZoneId, campaign.CampaignName, campaign.StartDate, campaign.EndDate, campaign.Status);
    }

    public async Task<AdCampaignDto> UpdateAsync(int campaignId, UpdateAdCampaignRequestDto request, CancellationToken ct = default)
    {
        var campaign = await db.AdCampaigns.FindAsync([campaignId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("AdCampaignNotFound", campaignId));

        var startDate = request.StartDate.GetValueOrDefault();
        var endDate = request.EndDate.GetValueOrDefault();

        if (endDate <= startDate)
            throw new ArgumentException(localizer.Get("CampaignDateInvalid"));

        campaign.RobotZoneId = request.RobotZoneId;
        campaign.CampaignName = request.CampaignName.Trim();
        campaign.StartDate = startDate;
        campaign.EndDate = endDate;
        campaign.Status = request.Status;
        await db.SaveChangesAsync(ct);
        return new AdCampaignDto(campaign.AdCampaignId, campaign.PackageId, campaign.BrandId,
            campaign.RobotZoneId, campaign.CampaignName, campaign.StartDate, campaign.EndDate, campaign.Status);
    }

    public async Task DeleteAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await db.AdCampaigns.FindAsync([campaignId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("AdCampaignNotFound", campaignId));

        db.AdCampaigns.Remove(campaign);
        await db.SaveChangesAsync(ct);
    }
}