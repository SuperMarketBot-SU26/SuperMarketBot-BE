using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdBroadcastService(
    AppDbContext db) : IAdBroadcastService
{
    public async Task<AdPlaylistDto> GetPlaylistForRobotAsync(
        int robotId, int x, int y, CancellationToken ct)
    {
        var assignment = await GetActiveAssignmentAsync(robotId, ct);
        if (assignment == null)
            return EmptyPlaylist(robotId, "None", null, null);

        var adRoute = await LoadAdRouteWithDataAsync(assignment.AdRouteId, ct);
        if (adRoute == null || !adRoute.IsActive)
            return EmptyPlaylist(robotId, "None", null, null);

        if (adRoute.IsAutonomous)
            return await GetAutonomousPlaylistByPositionAsync(robotId, adRoute, x, y, ct);
        else
            return await GetZoneShelfPlaylistAsync(robotId, x, y, ct);
    }

    public async Task<AdRouteBroadcastDto?> GetAutonomousRoutePlaylistAsync(
        int robotId, CancellationToken ct)
    {
        var assignment = await GetActiveAssignmentAsync(robotId, ct);
        if (assignment == null)
            return null;

        var adRoute = await LoadAdRouteWithDataAsync(assignment.AdRouteId, ct);
        if (adRoute == null || !adRoute.IsAutonomous || !adRoute.IsActive)
            return null;

        var now = DateTime.UtcNow;
        var routeNodes = adRoute.Nodes.OrderBy(n => n.SequenceOrder).ToList();
        var zoneIds = routeNodes
            .Where(n => n.ZoneId.HasValue)
            .Select(n => n.ZoneId!.Value)
            .Distinct()
            .ToList();

        var playlistByZone = await BuildPlaylistByZoneAsync(zoneIds, now, ct);

        var stops = routeNodes.Select(n =>
        {
            playlistByZone.TryGetValue(n.ZoneId ?? 0, out var playlist);
            return new AutonomousRouteStopDto(
                n.SequenceOrder,
                n.NodeId,
                n.Node?.NodeName,
                n.DwellTimeSeconds,
                n.ZoneId,
                n.Zone?.ZoneName,
                playlist ?? []);
        }).ToList();

        return new AdRouteBroadcastDto(
            robotId,
            adRoute.AdRouteId,
            adRoute.RouteName,
            adRoute.IsAutonomous,
            stops,
            now);
    }

    public async Task<AdPlaylistDto> GetZoneShelfPlaylistAsync(
        int robotId, int x, int y, CancellationToken ct)
    {
        var semanticObject = await db.SemanticObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(so =>
                so.XMin <= x && x <= so.XMax &&
                so.YMin <= y && y <= so.YMax, ct);

        if (semanticObject == null)
            return EmptyPlaylist(robotId, "ZoneShelf", null, null);

        var now = DateTime.UtcNow;
        var campaigns = await db.AdCampaigns
            .AsNoTracking()
            .Where(c => c.SemanticObjectId == semanticObject.ObjectId
                     && c.Status == CampaignStatus.Active
                     && c.StartDate <= now && c.EndDate >= now)
            .Include(c => c.AdResources.Where(r => r.Status == AdResourceStatus.Active))
            .ToListAsync(ct);

        if (campaigns.Count == 0)
            return EmptyPlaylist(robotId, "ZoneShelf", null, null);

        var resources = BuildPlaylistItems(campaigns);
        return new AdPlaylistDto(
            robotId,
            "ZoneShelf",
            null,
            null,
            null,
            resources,
            now);
    }

    public async Task<AdPlaylistDto> GetPlaylistForNodeAsync(
        int robotId, int nodeId, CancellationToken ct)
    {
        var node = await db.NavigationNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.NodeId == nodeId, ct);

        if (node == null)
            return EmptyPlaylist(robotId, "Autonomous", nodeId, null);

        var routeNode = await db.AdRouteNodes
            .AsNoTracking()
            .Include(rn => rn.Zone)
            .FirstOrDefaultAsync(rn => rn.NodeId == nodeId, ct);

        if (routeNode == null)
            return EmptyPlaylist(robotId, "Autonomous", nodeId, null);

        if (!routeNode.ZoneId.HasValue)
            return EmptyPlaylist(robotId, "Autonomous", nodeId, null);

        var now = DateTime.UtcNow;
        var campaigns = await db.AdCampaignZones
            .AsNoTracking()
            .Where(cz => cz.ZoneId == routeNode.ZoneId.Value)
            .Where(cz => cz.AdCampaign!.Status == CampaignStatus.Active
                      && cz.AdCampaign.StartDate <= now
                      && cz.AdCampaign.EndDate >= now)
            .Include(cz => cz.AdCampaign!.AdResources.Where(r => r.Status == AdResourceStatus.Active))
            .Select(cz => cz.AdCampaign!)
            .ToListAsync(ct);

        var resources = BuildPlaylistItems(campaigns);
        return new AdPlaylistDto(
            robotId,
            "Autonomous",
            nodeId,
            routeNode.ZoneId,
            routeNode.Zone?.ZoneName,
            resources,
            now);
    }

    private async Task<RobotAdRouteAssignment?> GetActiveAssignmentAsync(int robotId, CancellationToken ct)
    {
        return await db.RobotAdRouteAssignments
            .AsNoTracking()
            .Where(a => a.RobotId == robotId && a.Status == "Active")
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<AdRoute?> LoadAdRouteWithDataAsync(int adRouteId, CancellationToken ct)
    {
        return await db.AdRoutes
            .AsNoTracking()
            .Include(r => r.Nodes.OrderBy(n => n.SequenceOrder))
                .ThenInclude(n => n.Node)
            .Include(r => r.Nodes.Where(n => n.ZoneId != null))
                .ThenInclude(n => n.Zone)
            .Include(r => r.SemanticObject)
            .FirstOrDefaultAsync(r => r.AdRouteId == adRouteId, ct);
    }

    private async Task<AdPlaylistDto> GetAutonomousPlaylistByPositionAsync(
        int robotId, AdRoute adRoute, int x, int y, CancellationToken ct)
    {
        var currentNode = await db.NavigationNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.XCoord == x && n.YCoord == y, ct);

        if (currentNode == null)
            return EmptyPlaylist(robotId, "Autonomous", null, null);

        return await GetPlaylistForNodeAsync(robotId, currentNode.NodeId, ct);
    }

    private async Task<Dictionary<int, List<RobotPlaylistItemDto>>> BuildPlaylistByZoneAsync(
        List<int> zoneIds, DateTime now, CancellationToken ct)
    {
        if (zoneIds.Count == 0)
            return [];

        var allItems = await (from sp in db.SponsoredProducts.AsNoTracking()
                             join acz in db.AdCampaignZones.AsNoTracking()
                                 on sp.AdCampaignId equals acz.AdCampaignId
                             where zoneIds.Contains(acz.ZoneId)
                                   && sp.Status == SponsoredProductStatus.Active
                                   && sp.AdCampaign!.Status == CampaignStatus.Active
                                   && sp.AdCampaign.StartDate <= now
                                   && sp.AdCampaign.EndDate >= now
                             orderby sp.AdCampaign!.Package!.AdScore descending,
                                     sp.Priority descending
                             select new
                             {
                                 acz.ZoneId,
                                 Item = new RobotPlaylistItemDto
                                 {
                                     SponsoredId = sp.SponsoredId,
                                     AdCampaignId = sp.AdCampaignId,
                                     CampaignName = sp.AdCampaign!.CampaignName,
                                     ProductId = sp.ProductId,
                                     ProductName = sp.Product!.ProductName,
                                     ProductPrice = sp.Product.UnitPrice,
                                     Priority = sp.Priority,
                                     AdScore = sp.AdCampaign.Package!.AdScore,
                                     EndDate = sp.AdCampaign!.EndDate,
                                     ImageUrl = sp.Product!.ImageUrl ?? string.Empty,
                                     DisplayDurationSeconds = 30,
                                     MediaContents = sp.AdCampaign!.AdResources
                                         .Where(r => r.Status == AdResourceStatus.Active)
                                         .Select(r => new MediaContentDto(
                                             r.ResourceType,
                                             r.ResourceUrl!,
                                             r.ContentText,
                                             r.Resolution))
                                         .ToList()
                                 }
                             })
                             .ToListAsync(ct);

        return allItems
            .GroupBy(x => x.ZoneId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item).ToList());
    }

    private static List<RobotPlaylistItemDto> BuildPlaylistItems(List<AdCampaign> campaigns)
    {
        return campaigns
            .SelectMany(c => c.AdResources
                .Where(r => r.Status == AdResourceStatus.Active)
                .Select(r => new RobotPlaylistItemDto
                {
                    SponsoredId = 0,
                    AdCampaignId = c.AdCampaignId,
                    CampaignName = c.CampaignName,
                    ProductId = 0,
                    ProductName = r.ContentText ?? c.CampaignName,
                    ProductPrice = 0,
                    Priority = 0,
                    AdScore = 0,
                    EndDate = c.EndDate,
                    ImageUrl = r.ResourceUrl ?? string.Empty,
                    DisplayDurationSeconds = 30,
                    MediaContents =
                    [
                        new MediaContentDto(r.ResourceType, r.ResourceUrl ?? string.Empty, r.ContentText, r.Resolution)
                    ]
                }))
            .ToList();
    }

    private static AdPlaylistDto EmptyPlaylist(int robotId, string mode, int? nodeId, int? zoneId) =>
        new(robotId, mode, nodeId, zoneId, null, [], DateTime.UtcNow);
}
