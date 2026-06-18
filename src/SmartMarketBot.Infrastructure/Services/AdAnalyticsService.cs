using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

internal sealed class ActiveCampaignRow
{
    public int AdCampaignId { get; set; }
    public int? RobotZoneId { get; set; }
    public decimal PriceRoute { get; set; }
}

/// <summary>
/// Phase B - Flow 1: Route-based impression recording.
/// Mỗi lần robot tới Slot thuộc Zone có <c>AdCampaign.RobotZoneId</c> active,
/// ghi 1 <c>AdCampaignLog</c> (ActionType='RoutePass') cho từng Sponsored product trong campaign.
/// Charge = <c>AdPackage.PriceRoute</c> cho 1 lượt chạy qua (Route-based billing).
/// </summary>
public sealed class AdAnalyticsService(AppDbContext db, ILocalizationService localizer) : IAdAnalyticsService
{
    public async Task<RouteImpressionResponseDto> RecordRoutePassAsync(
        string robotCode, RouteImpressionRequestDto request, CancellationToken ct = default)
    {
        // 1. Tìm Robot theo code
        var robot = await db.Robots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RobotCode == robotCode, ct)
            ?? throw new KeyNotFoundException(localizer.Get("AdRobotNotFoundByCode", robotCode));

        // 2. Tìm Slot + Zone (Slot → Shelf → Aisle → Zone)
        var slotInfo = await (
            from s in db.Slots.AsNoTracking()
            join sh in db.Shelves.AsNoTracking() on s.ShelfId equals sh.ShelfId
            join ai in db.Aisles.AsNoTracking() on sh.AisleId equals ai.AisleId
            join z in db.Zones.AsNoTracking() on ai.ZoneId equals z.ZoneId
            where s.SlotId == request.SlotId
            select new { s.SlotId, z.ZoneId, s.SlotCode }
        ).FirstOrDefaultAsync(ct);

        if (slotInfo is null)
            throw new KeyNotFoundException(localizer.Get("AdSlotNotFound", request.SlotId));

        // 3. Tìm các AdCampaign.RobotZoneId ứng với Zone này
        var zoneRobotIds = await db.RobotZones
            .AsNoTracking()
            .Where(rz => rz.ZoneId == slotInfo.ZoneId)
            .Select(rz => rz.RobotZoneId)
            .ToListAsync(ct);

        // 4. Tìm AdCampaigns active trong Zone
        var nowUtc = DateTime.UtcNow;
        var activeCampaigns = new List<ActiveCampaignRow>();

        if (zoneRobotIds.Count > 0)
        {
            activeCampaigns = await (
                from ac in db.AdCampaigns.AsNoTracking()
                join pkg in db.AdPackages.AsNoTracking() on ac.PackageId equals pkg.PackageId
                where ac.Status == "Running"
                      && ac.StartDate <= nowUtc
                      && ac.EndDate >= nowUtc
                      && ac.RobotZoneId != null
                      && zoneRobotIds.Contains(ac.RobotZoneId.Value)
                select new ActiveCampaignRow
                {
                    AdCampaignId = ac.AdCampaignId,
                    RobotZoneId = ac.RobotZoneId,
                    PriceRoute = pkg.PriceRoute
                }
            ).ToListAsync(ct);
        }

        if (activeCampaigns.Count == 0)
        {
            return new RouteImpressionResponseDto(
                robotCode,
                request.SlotId,
                slotInfo.ZoneId,
                0,
                0m,
                [],
                localizer.Get("AdNoActiveCampaign", slotInfo.ZoneId));
        }

        // 5. Với mỗi campaign, lấy Sponsored products Active
        var campaignIds = activeCampaigns.Select(c => c.AdCampaignId).ToList();
        var sponsoredList = await db.SponsoredProducts
            .AsNoTracking()
            .Where(sp => campaignIds.Contains(sp.AdCampaignId) && sp.Status == "Active")
            .Select(sp => new { sp.SponsoredId, sp.AdCampaignId, sp.ProductId })
            .ToListAsync(ct);

        // 6. Ghi AdCampaignLog (RoutePass) cho từng Sponsored
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
                    RobotZoneId = camp.RobotZoneId,
                    ZoneId = slotInfo.ZoneId,
                    SlotId = slotInfo.SlotId,
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
            slotInfo.ZoneId,
            responseItems.Count,
            totalCharged,
            responseItems,
            localizer.Get("AdImpressionRecorded", responseItems.Count, campaignIds.FirstOrDefault()));
    }
}
