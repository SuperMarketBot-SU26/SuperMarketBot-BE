using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdResourceService(
    AppDbContext db,
    ILocalizationService localizer) : IAdResourceService
{
    public async Task<PaginatedResponse<AdResourceDto>> GetByCampaignAsync(
        int campaignId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.AdResources
            .AsNoTracking()
            .Where(r => r.AdCampaignId == campaignId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.ResourceId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdResourceDto(
                r.ResourceId,
                r.AdCampaignId,
                r.ResourceType,
                r.ResourceUrl,
                r.ContentText,
                r.Resolution,
                r.Status))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<AdResourceDto>(
            items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<AdResourceDto?> GetByIdAsync(int resourceId, CancellationToken cancellationToken = default)
    {
        var r = await db.AdResources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == resourceId, cancellationToken);

        if (r is null)
            return null;

        return new AdResourceDto(
            r.ResourceId,
            r.AdCampaignId,
            r.ResourceType,
            r.ResourceUrl,
            r.ContentText,
            r.Resolution,
            r.Status);
    }

    public async Task<AdResourceDto> CreateAsync(
        CreateAdResourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var campaignExists = await db.AdCampaigns
            .AnyAsync(c => c.AdCampaignId == request.AdCampaignId, cancellationToken);

        if (!campaignExists)
            throw new KeyNotFoundException(localizer.Get("Error_CampaignNotFound", request.AdCampaignId.ToString()));

        var resource = new AdResource
        {
            AdCampaignId = request.AdCampaignId,
            ResourceType = request.ResourceType,
            ResourceUrl = request.ResourceUrl ?? string.Empty,
            ContentText = request.ContentText,
            Resolution = request.Resolution,
            Status = AdResourceStatus.Active
        };

        db.AdResources.Add(resource);
        await db.SaveChangesAsync(cancellationToken);

        return new AdResourceDto(
            resource.ResourceId,
            resource.AdCampaignId,
            resource.ResourceType,
            resource.ResourceUrl,
            resource.ContentText,
            resource.Resolution,
            resource.Status);
    }

    public async Task<AdResourceDto> UpdateStatusAsync(
        int resourceId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var r = await db.AdResources
            .FirstOrDefaultAsync(x => x.ResourceId == resourceId, cancellationToken);

        if (r is null)
            throw new KeyNotFoundException(localizer.Get("Error_ResourceNotFound", resourceId.ToString()));

        r.Status = status;
        await db.SaveChangesAsync(cancellationToken);

        return new AdResourceDto(
            r.ResourceId,
            r.AdCampaignId,
            r.ResourceType,
            r.ResourceUrl,
            r.ContentText,
            r.Resolution,
            r.Status);
    }

    public async Task<bool> DeleteAsync(int resourceId, CancellationToken cancellationToken = default)
    {
        var r = await db.AdResources
            .FirstOrDefaultAsync(x => x.ResourceId == resourceId, cancellationToken);

        if (r is null)
            return false;

        db.AdResources.Remove(r);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
