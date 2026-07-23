using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Domain.Enums;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

internal sealed class ActiveCampaignRow
{
    public int AdCampaignId { get; set; }
    public int? SemanticObjectId { get; set; }

    /// <summary>Số tiền charge cho impression này (đã lấy max của các snapshot trùng campaign).</summary>
    public decimal ChargedAmount { get; set; }

    /// <summary>Loại targeting trúng: Route | Zone | Shelf — dùng để log debug.</summary>
    public string HitSource { get; set; } = string.Empty;
}

/// <summary>
/// Phase B - Flow 1: Route-based impression recording — hỗ trợ 3 luồng targeting độc lập.
/// Robot gửi tọa độ (X, Y) → tìm SemanticObject chứa tọa độ + Zone
/// → tìm route hiện tại của robot (RouteAssignment.Status = Active)
/// → UNION 3 tập campaign:
///     1. AdCampaignRoute (RobotRouteId trùng route hiện tại)
///     2. AdCampaignZone  (ZoneId trùng zone hiện tại)
///     3. AdCampaign.SemanticObjectId (kệ hiện tại)
/// → dedupe theo AdCampaignId, charge theo snapshot lớn nhất trùng khớp.
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

        // Tìm SemanticObject chứa tọa độ (X, Y) — đại diện cho kệ hiện tại.
        var semanticObject = await db.SemanticObjects
            .AsNoTracking()
            .Include(so => so.ProductType)
            .FirstOrDefaultAsync(so =>
                so.ObjectType == SemanticObjectType.Shelf &&
                so.XMin <= request.XCoord && so.XMax >= request.XCoord &&
                so.YMin <= request.YCoord && so.YMax >= request.YCoord, ct);

        // Tìm Zone chứa slotId.
        var zone = await db.Zones
            .AsNoTracking()
            .Include(z => z.Aisles)
                .ThenInclude(a => a.Shelves)
            .FirstOrDefaultAsync(z => z.Aisles.Any(a => a.Shelves.Any(s => s.Slots.Any(slot =>
                slot.SlotId == request.SlotId))), ct);

        // Tìm route hiện tại của robot.
        var currentRouteId = await db.RouteAssignments
            .AsNoTracking()
            .Where(ra => ra.RobotId == robot.RobotId && ra.Status == "Active")
            .OrderByDescending(ra => ra.AssignedAt)
            .Select(ra => (int?)ra.RobotRouteId)
            .FirstOrDefaultAsync(ct);

        var nowUtc = DateTime.UtcNow;
        var hits = new Dictionary<int, ActiveCampaignRow>(); // AdCampaignId → row đã chọn

        // ── Luồng 1: Route targeting ─────────────────────────────────────────
        if (currentRouteId.HasValue)
        {
            var byRoute = await (
                from ac in db.AdCampaigns.AsNoTracking()
                join acr in db.AdCampaignRoutes.AsNoTracking() on ac.AdCampaignId equals acr.AdCampaignId
                where acr.RobotRouteId == currentRouteId.Value
                      && ac.Status == CampaignStatus.Active
                      && ac.StartDate <= nowUtc
                      && ac.EndDate >= nowUtc
                select new { ac.AdCampaignId, ac.SemanticObjectId, Charge = acr.RoutePriceCharged }
            ).ToListAsync(ct);

            foreach (var row in byRoute)
            {
                if (!hits.TryGetValue(row.AdCampaignId, out var existing) || row.Charge > existing.ChargedAmount)
                {
                    hits[row.AdCampaignId] = new ActiveCampaignRow
                    {
                        AdCampaignId = row.AdCampaignId,
                        SemanticObjectId = row.SemanticObjectId,
                        ChargedAmount = row.Charge,
                        HitSource = "Route"
                    };
                }
            }
        }

        // ── Luồng 2: Zone targeting ──────────────────────────────────────────
        if (zone != null)
        {
            var byZone = await (
                from ac in db.AdCampaigns.AsNoTracking()
                join acz in db.AdCampaignZones.AsNoTracking() on ac.AdCampaignId equals acz.AdCampaignId
                where acz.ZoneId == zone.ZoneId
                      && ac.Status == CampaignStatus.Active
                      && ac.StartDate <= nowUtc
                      && ac.EndDate >= nowUtc
                select new { ac.AdCampaignId, ac.SemanticObjectId, Charge = acz.ZonePriceCharged }
            ).ToListAsync(ct);

            foreach (var row in byZone)
            {
                if (!hits.TryGetValue(row.AdCampaignId, out var existing))
                {
                    hits[row.AdCampaignId] = new ActiveCampaignRow
                    {
                        AdCampaignId = row.AdCampaignId,
                        SemanticObjectId = row.SemanticObjectId,
                        ChargedAmount = row.Charge,
                        HitSource = "Zone"
                    };
                }
                else if (row.Charge > existing.ChargedAmount)
                {
                    existing.ChargedAmount = row.Charge;
                    existing.HitSource += "+Zone";
                }
            }
        }

        // ── Luồng 3: Shelf targeting ─────────────────────────────────────────
        if (semanticObject != null)
        {
            var byShelf = await (
                from ac in db.AdCampaigns.AsNoTracking()
                where ac.SemanticObjectId == semanticObject.ObjectId
                      && ac.Status == CampaignStatus.Active
                      && ac.StartDate <= nowUtc
                      && ac.EndDate >= nowUtc
                select new { ac.AdCampaignId, ac.SemanticObjectId, Charge = ac.ShelfPriceCharged }
            ).ToListAsync(ct);

            foreach (var row in byShelf)
            {
                if (!hits.TryGetValue(row.AdCampaignId, out var existing))
                {
                    hits[row.AdCampaignId] = new ActiveCampaignRow
                    {
                        AdCampaignId = row.AdCampaignId,
                        SemanticObjectId = row.SemanticObjectId,
                        ChargedAmount = row.Charge,
                        HitSource = "Shelf"
                    };
                }
                else if (row.Charge > existing.ChargedAmount)
                {
                    existing.ChargedAmount = row.Charge;
                    existing.HitSource += "+Shelf";
                }
            }
        }

        if (hits.Count == 0)
        {
            var reason = !currentRouteId.HasValue && zone == null && semanticObject == null
                ? localizer.Get("AdNoContext", robotCode)
                : !currentRouteId.HasValue && hits.Count == 0
                    ? localizer.Get("AdRobotNoActiveRoute", robotCode)
                    : localizer.Get("AdNoActiveCampaign", semanticObject?.ObjectId ?? 0);

            return new RouteImpressionResponseDto(
                robotCode,
                request.SlotId,
                semanticObject?.ObjectId,
                0,
                0m,
                [],
                reason);
        }

        var activeCampaigns = hits.Values.ToList();
        var campaignIds = activeCampaigns.Select(c => c.AdCampaignId).ToList();
        var sponsoredList = await db.SponsoredProducts
            .AsNoTracking()
            .Where(sp => campaignIds.Contains(sp.AdCampaignId) && sp.Status == SponsoredProductStatus.Active)
            .Select(sp => new { sp.SponsoredId, sp.AdCampaignId, sp.ProductId })
            .ToListAsync(ct);

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
                    ChargedAmount = camp.ChargedAmount,
                    SponsoredId = sp.SponsoredId,
                    ProductId = sp.ProductId,
                    RobotId = robot.RobotId,
                    SemanticObjectId = semanticObject?.ObjectId,
                    ZoneId = zone?.ZoneId,
                    SlotId = request.SlotId,
                    MemberId = request.MemberId,
                    XCoord = request.XCoord,
                    YCoord = request.YCoord
                };
                logs.Add(log);
                totalCharged += camp.ChargedAmount;
                responseItems.Add(new RouteImpressionLogItem(
                    sp.SponsoredId, sp.ProductId, camp.AdCampaignId, camp.ChargedAmount));
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
