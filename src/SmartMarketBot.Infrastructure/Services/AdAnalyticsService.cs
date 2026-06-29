using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

internal sealed class ActiveCampaignRow
{
    public int AdCampaignId { get; set; }
    public int? SemanticObjectId { get; set; }
    public decimal PriceRoute { get; set; }
}

/// <summary>
/// Phase B - Flow 1: Route-based impression recording dựa trên SemanticObject.
/// Robot gửi tọa độ (X, Y) → tìm SemanticObject chứa tọa độ
/// → tìm AdCampaign liên kết → ghi impression cho SponsoredProducts.
/// </summary>
public sealed class AdAnalyticsService(AppDbContext db, ILocalizationService localizer) : IAdAnalyticsService
{
    public async Task<RouteImpressionResponseDto> RecordRoutePassAsync(
        string robotCode, RouteImpressionRequestDto request, CancellationToken ct = default)
    {
        var robot = await db.Robots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RobotCode == robotCode, ct)
            ?? throw new KeyNotFoundException(localizer.Get("AdRobotNotFoundByCode", robotCode));

        // Tìm SemanticObject chứa tọa độ (X, Y)
        var semanticObject = await db.SemanticObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(so =>
                so.ObjectType == "shelf" &&
                so.XMin <= request.XCoord && so.XMax >= request.XCoord &&
                so.YMin <= request.YCoord && so.YMax >= request.YCoord, ct);

        var nowUtc = DateTime.UtcNow;

        // Tìm các AdCampaign active liên kết với SemanticObject này
        List<ActiveCampaignRow> activeCampaigns = [];

        if (semanticObject != null)
        {
            activeCampaigns = await (
                from ac in db.AdCampaigns.AsNoTracking()
                join pkg in db.AdPackages.AsNoTracking() on ac.PackageId equals pkg.PackageId
                where ac.Status == CampaignStatus.Active
                      && ac.StartDate <= nowUtc
                      && ac.EndDate >= nowUtc
                      && ac.SemanticObjectId == semanticObject.ObjectId
                select new ActiveCampaignRow
                {
                    AdCampaignId = ac.AdCampaignId,
                    SemanticObjectId = ac.SemanticObjectId,
                    PriceRoute = pkg.PriceRoute
                }
            ).ToListAsync(ct);
        }

        if (activeCampaigns.Count == 0)
        {
            return new RouteImpressionResponseDto(
                robotCode,
                request.SlotId,
                null,
                0,
                0m,
                [],
                localizer.Get("AdNoActiveCampaign", semanticObject?.ObjectId ?? 0));
        }

        // Lấy SponsoredProducts Active cho các campaign
        var campaignIds = activeCampaigns.Select(c => c.AdCampaignId).ToList();
        var sponsoredList = await db.SponsoredProducts
            .AsNoTracking()
            .Where(sp => campaignIds.Contains(sp.AdCampaignId) && sp.Status == SponsoredProductStatus.Active)
            .Select(sp => new { sp.SponsoredId, sp.AdCampaignId, sp.ProductId })
            .ToListAsync(ct);

        // Ghi AdCampaignLog (RoutePass)
        var logs = new List<AdCampaignLog>();
        var responseItems = new List<RouteImpressionLogItem>();
        var totalCharged = 0m;

        foreach (var camp in activeCampaigns)
        {
            var spList = sponsoredList.Where(s => s.AdCampaignId == camp.AdCampaignId);
            foreach (var sp in spList)
            {
                var log = new AdCampaignLog
                {
                    AdCampaignId = camp.AdCampaignId,
                    ActionType = "RoutePass",
                    ChargedAmount = camp.PriceRoute,
                    SponsoredId = sp.SponsoredId,
                    ProductId = sp.ProductId,
                    RobotId = robot.RobotId,
                    SemanticObjectId = semanticObject?.ObjectId,
                    SlotId = request.SlotId,
                    MemberId = request.MemberId,
                    XCoord = request.XCoord,
                    YCoord = request.YCoord
                };
                logs.Add(log);
                totalCharged += camp.PriceRoute;
                responseItems.Add(new RouteImpressionLogItem(
                    sp.SponsoredId, sp.ProductId, camp.AdCampaignId, camp.PriceRoute));
            }
        }

        if (logs.Count > 0)
        {
            db.AdCampaignLogs.AddRange(logs);
            await db.SaveChangesAsync(ct);
        }

        return new RouteImpressionResponseDto(
            robotCode,
            request.SlotId,
            semanticObject?.ObjectId,
            responseItems.Count,
            totalCharged,
            responseItems,
            localizer.Get("AdImpressionRecorded", responseItems.Count, campaignIds.FirstOrDefault()));
    }
}
