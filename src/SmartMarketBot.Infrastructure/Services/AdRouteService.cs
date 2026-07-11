using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdRouteService(
    AppDbContext db,
    ILocalizationService localizer,
    ILogger<AdRouteService> logger) : IAdRouteService
{
    public async Task<PaginatedResponse<AdRouteResponseDto>> GetListAsync(
        int pageNumber, int pageSize, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = db.AdRoutes.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var routes = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Nodes)
                .ThenInclude(n => n.Node)
            .Include(r => r.Campaigns)
            .ToListAsync(cancellationToken);

        var items = routes.Select(MapToDto).ToList();

        return new PaginatedResponse<AdRouteResponseDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<AdRouteResponseDto?> GetByIdAsync(int routeId, CancellationToken cancellationToken = default)
    {
        var route = await db.AdRoutes
            .AsNoTracking()
            .Include(r => r.Nodes.OrderBy(n => n.SequenceOrder))
                .ThenInclude(n => n.Node)
            .Include(r => r.Campaigns)
            .FirstOrDefaultAsync(r => r.AdRouteId == routeId, cancellationToken);

        return route == null ? null : MapToDto(route);
    }

    public async Task<AdRouteResponseDto> CreateAsync(
        CreateAdRouteRequestDto request, CancellationToken cancellationToken = default)
    {
        // Validate nodes exist
        var nodeIds = request.Nodes.Select(n => n.NodeId).ToList();
        var existingNodes = await db.NavigationNodes
            .Where(n => nodeIds.Contains(n.NodeId))
            .Select(n => n.NodeId)
            .ToListAsync(cancellationToken);

        var missingNodes = nodeIds.Except(existingNodes).ToList();
        if (missingNodes.Count != 0)
            throw new KeyNotFoundException(localizer.Get("NavigationNodesNotFound", string.Join(", ", missingNodes)));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var route = new AdRoute
            {
                RouteName = request.RouteName,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.AdRoutes.Add(route);
            await db.SaveChangesAsync(cancellationToken);

            // Add route nodes
            var routeNodes = request.Nodes.Select((n, idx) => new AdRouteNode
            {
                AdRouteId = route.AdRouteId,
                NodeId = n.NodeId,
                SequenceOrder = n.SequenceOrder > 0 ? n.SequenceOrder : idx + 1,
                DwellTimeSeconds = n.DwellTimeSeconds > 0 ? n.DwellTimeSeconds : 30
            });
            db.AdRouteNodes.AddRange(routeNodes);
            await db.SaveChangesAsync(cancellationToken);

            // Add campaigns
            if (request.CampaignIds is { Count: > 0 })
            {
                var campaigns = request.CampaignIds.Distinct().Select(campaignId => new AdRouteCampaign
                {
                    AdRouteId = route.AdRouteId,
                    AdCampaignId = campaignId
                });
                db.AdRouteCampaigns.AddRange(campaigns);
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("AdRoute {RouteId} created with {NodeCount} nodes", route.AdRouteId, request.Nodes.Count);

            return (await GetByIdAsync(route.AdRouteId, cancellationToken))!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AdRouteResponseDto> UpdateAsync(
        int routeId, UpdateAdRouteRequestDto request, CancellationToken cancellationToken = default)
    {
        var route = await db.AdRoutes
            .Include(r => r.Nodes)
            .Include(r => r.Campaigns)
            .FirstOrDefaultAsync(r => r.AdRouteId == routeId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdRouteNotFound", routeId));

        route.RouteName = request.RouteName;
        route.Description = request.Description;
        route.IsActive = request.IsActive;

        // Update nodes if provided
        if (request.Nodes != null)
        {
            // Remove existing nodes
            db.AdRouteNodes.RemoveRange(route.Nodes);

            if (request.Nodes.Count > 0)
            {
                var newNodes = request.Nodes.Select((n, idx) => new AdRouteNode
                {
                    AdRouteId = routeId,
                    NodeId = n.NodeId,
                    SequenceOrder = n.SequenceOrder > 0 ? n.SequenceOrder : idx + 1,
                    DwellTimeSeconds = n.DwellTimeSeconds > 0 ? n.DwellTimeSeconds : 30
                });
                db.AdRouteNodes.AddRange(newNodes);
            }
        }

        // Update campaigns if provided
        if (request.CampaignIds != null)
        {
            db.AdRouteCampaigns.RemoveRange(route.Campaigns);

            if (request.CampaignIds.Count > 0)
            {
                var newCampaigns = request.CampaignIds.Distinct().Select(campaignId => new AdRouteCampaign
                {
                    AdRouteId = routeId,
                    AdCampaignId = campaignId
                });
                db.AdRouteCampaigns.AddRange(newCampaigns);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AdRoute {RouteId} updated", routeId);

        return (await GetByIdAsync(routeId, cancellationToken))!;
    }

    public async Task<bool> DeleteAsync(int routeId, CancellationToken cancellationToken = default)
    {
        var route = await db.AdRoutes.FindAsync([routeId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdRouteNotFound", routeId));

        db.AdRoutes.Remove(route);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AdRoute {RouteId} deleted", routeId);
        return true;
    }

    public async Task<AdRouteResponseDto> AssignToRobotAsync(
        int routeId, int robotId, CancellationToken cancellationToken = default)
    {
        var route = await db.AdRoutes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AdRouteId == routeId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdRouteNotFound", routeId));

        if (!route.IsActive)
            throw new InvalidOperationException(localizer.Get("CannotAssignInactiveRoute"));

        var robot = await db.Robots.FindAsync([robotId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("RobotNotFound", robotId));

        // Deactivate existing assignments for this robot
        var existingAssignments = await db.RouteAssignments
            .Where(ra => ra.RobotId == robotId && ra.Status == "Active")
            .ToListAsync(cancellationToken);

        foreach (var assignment in existingAssignments)
            assignment.Status = "Completed";

        // Create new assignment
        var newAssignment = new RouteAssignment
        {
            RobotId = robotId,
            RobotRouteId = routeId,
            AssignedAt = DateTime.UtcNow,
            Status = "Active"
        };

        db.RouteAssignments.Add(newAssignment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AdRoute {RouteId} assigned to Robot {RobotId}", routeId, robotId);

        return (await GetByIdAsync(routeId, cancellationToken))!;
    }

    public async Task<AdRouteResponseDto?> GetActiveRouteForRobotAsync(
        int robotId, CancellationToken cancellationToken = default)
    {
        var assignment = await db.RouteAssignments
            .AsNoTracking()
            .Where(ra => ra.RobotId == robotId && ra.Status == "Active")
            .OrderByDescending(ra => ra.AssignedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment == null)
            return null;

        return await GetByIdAsync(assignment.RobotRouteId, cancellationToken);
    }

    private static AdRouteResponseDto MapToDto(AdRoute route)
    {
        return new AdRouteResponseDto(
            route.AdRouteId,
            route.RouteName,
            route.Description,
            route.IsActive,
            route.CreatedAt,
            route.Nodes.OrderBy(n => n.SequenceOrder).Select(n => new AdRouteNodeDto(
                n.AdRouteNodeId,
                n.NodeId,
                n.Node?.NodeName,
                n.SequenceOrder,
                n.DwellTimeSeconds
            )).ToList(),
            route.Campaigns.Select(c => c.AdCampaignId).ToList());
    }
}
