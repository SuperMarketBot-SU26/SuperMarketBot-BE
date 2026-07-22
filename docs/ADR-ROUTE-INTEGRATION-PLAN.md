# AdRoute Integration Plan

## Overview

This document outlines the plan to integrate `AdRoute` into the robot's advertising system, replacing the removed `RouteType` concept with a more flexible, mode-based approach.

---

## 1. Current State

### Existing Schema

```
┌─────────────────┐       ┌──────────────────────┐       ┌─────────────────┐
│   RobotRoute    │       │   AdCampaignRoute    │       │   AdCampaign    │
│─────────────────│       │──────────────────────│       │─────────────────│
│ RobotRouteId    │◄──────│ AdCampaignId         │       │ AdCampaignId    │
│ RobotId         │       │ RobotRouteId         │──────►│ SemanticObjectId│ ← Zone/Shelf targeting
│ MapId           │       │ RoutePriceCharged    │       │ Status          │
│ RouteName       │       └──────────────────────┘       │ AdResources[]   │
│ ZoneId          │                                       └─────────────────┘
│ Description     │                                              │
└─────────────────┘                                              │
                                                                  ▼
                                                         ┌─────────────────┐
                                                         │ SemanticObject  │
                                                         │─────────────────│
                                                         │ ObjectId        │
                                                         │ XMin, YMin      │
                                                         │ XMax, YMax      │ ← AABB for spatial detection
                                                         │ ObjectType      │
                                                         │ ProductTypeId   │
                                                         └─────────────────┘

┌─────────────────┐       ┌─────────────────┐
│    AdRoute      │       │  AdRouteNode     │
│─────────────────│       │─────────────────│
│ AdRouteId       │◄──────│ AdRouteId        │
│ RouteName       │       │ NodeId           │──────► NavigationNode
│ Description     │       │ SequenceOrder    │
│ IsActive        │       │ DwellTimeSeconds│
│ CreatedAt       │       └─────────────────┘
└─────────────────┘
         │
         ▼
┌─────────────────────┐
│  AdRouteCampaign    │
│─────────────────────│
│ AdRouteId           │──────► AdRoute
│ AdCampaignId       │──────► AdCampaign
└─────────────────────┘
```

### Problem

- `RobotRoute` is for patrol/navigation
- `AdRoute` is separate for advertising
- No clear mechanism to distinguish **Patrol** vs **Ad** routes
- No distinction between **Autonomous** and **Zone/Shelf** ad modes

---

## 2. Target Architecture

### New Schema

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ROBOT & ROUTE RELATIONSHIPS                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────┐    RouteAssignment     ┌──────────────┐    AdRouteAssignment   │
│  │  Robot  │───────────────────────►│ RobotRoute   │                       │
│  └─────────┘    (Patrol/Nav)       └──────────────┘                       │
│       │                                     │                              │
│       │        (Separation of concerns)     │                              │
│       │                                     │                              │
│       │        ┌──────────────┐    ┌──────────────────────┐                │
│       └───────►│ AdRoute      │◄───│ RobotAdRouteAssignment│               │
│                └──────────────┘    └──────────────────────┘                │
│                     │                                                    │
│                     │  Modes                                             │
│                     ▼                                                    │
│         ┌───────────────────────────────────────┐                        │
│         │           AdRoute                     │                        │
│         │  ┌─────────────────────────────────┐ │                        │
│         │  │ IsAutonomous: bool               │ │                        │
│         │  │ SemanticObjectId: int?           │ │ ← Zone/Shelf targeting │
│         │  │ Nodes[]: AdRouteNode             │ │ ← For Autonomous      │
│         │  │ Campaigns[]: AdRouteCampaign     │ │ ← Ad sources          │
│         │  └─────────────────────────────────┘ │                        │
│         └───────────────────────────────────────┘                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           AD BROADCAST MODES                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   AUTONOMOUS MODE                         ZONE/SHELF MODE                   │
│   ├── Trigger: Robot at node             │ ├── Trigger: Robot inside AABB  │
│   ├── Source: Playlist per stop           │ │   of SemanticObject           │
│   ├── Sequential: Play in order          │ ├── Source: AdCampaign linked   │
│   └── Example: "Stop 1 → Zone A ads"      │ │   to that SemanticObject      │
│                                           │ └── Dynamic: changes by location│
│                                           │     Example: "Near Produce →   │
│                                           │     show fresh product ads"     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Schema Changes Required

### 3.1 Modify `AdRoute` Entity

**File:** `Domain/Entities/AdRoute.cs`

```csharp
public class AdRoute
{
    public int AdRouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // === NEW: Ad Mode ===
    /// <summary>
    /// If true: Autonomous mode (sequential playlist from stops)
    /// If false: Zone/Shelf mode (AABB spatial detection)
    /// </summary>
    public bool IsAutonomous { get; set; } = false;

    /// <summary>
    /// For Zone/Shelf mode: target SemanticObject to track.
    /// When robot enters this object's AABB, broadcast its ads.
    /// Null = use all campaigns linked via AdRouteCampaign.
    /// </summary>
    public int? SemanticObjectId { get; set; }

    // Navigation
    public virtual SemanticObject? SemanticObject { get; set; }
    public virtual ICollection<AdRouteNode> Nodes { get; set; } = new List<AdRouteNode>();
    public virtual ICollection<AdRouteCampaign> Campaigns { get; set; } = new List<AdRouteCampaign>();
    public virtual ICollection<RobotAdRouteAssignment> Assignments { get; set; } = new List<RobotAdRouteAssignment>();
}
```

### 3.2 Modify `AdRouteNode` Entity

**File:** `Domain/Entities/AdRouteNode.cs`

```csharp
public class AdRouteNode
{
    public int AdRouteNodeId { get; set; }
    public int AdRouteId { get; set; }
    public int NodeId { get; set; }
    public int SequenceOrder { get; set; }
    public int DwellTimeSeconds { get; set; } = 30;

    // === NEW: Zone linking for playlist grouping ===
    /// <summary>
    /// ZoneId of the aisle this node belongs to.
    /// Used to group nodes into playlists for Autonomous mode.
    /// </summary>
    public int? ZoneId { get; set; }

    // Navigation
    public virtual AdRoute? AdRoute { get; set; }
    public virtual NavigationNode? Node { get; set; }
    public virtual Zone? Zone { get; set; }
}
```

### 3.3 New Entity: `RobotAdRouteAssignment`

**File:** `Domain/Entities/RobotAdRouteAssignment.cs`

```csharp
public class RobotAdRouteAssignment
{
    public int AssignmentId { get; set; }
    public int RobotId { get; set; }
    public int AdRouteId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";  // Active | Paused | Completed

    // Navigation
    public virtual Robot? Robot { get; set; }
    public virtual AdRoute? AdRoute { get; set; }
}
```

### 3.4 Database Migration

**Migration Name:** `AddAdRouteModeAndAssignment`

```sql
-- Add IsAutonomous and SemanticObjectId to AD_ROUTE
ALTER TABLE AD_ROUTE
ADD IsAutonomous BIT NOT NULL DEFAULT 0,
    SemanticObjectID INT NULL;

ALTER TABLE AD_ROUTE
ADD CONSTRAINT FK_AD_ROUTE_SEMANTIC_OBJECT
    FOREIGN KEY (SemanticObjectID) REFERENCES SEMANTIC_OBJECT(ObjectID)
    ON DELETE SET NULL;

-- Add ZoneId to AD_ROUTE_NODE
ALTER TABLE AD_ROUTE_NODE
ADD ZoneID INT NULL;

ALTER TABLE AD_ROUTE_NODE
ADD CONSTRAINT FK_AD_ROUTE_NODE_ZONE
    FOREIGN KEY (ZoneID) REFERENCES ZONE(ZoneID)
    ON DELETE SET NULL;

-- Create ROBOT_AD_ROUTE_ASSIGNMENT
CREATE TABLE ROBOT_AD_ROUTE_ASSIGNMENT (
    AssignmentID INT IDENTITY(1,1) PRIMARY KEY,
    RobotID INT NOT NULL,
    AdRouteID INT NOT NULL,
    AssignedAt DATETIME NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
    CONSTRAINT FK_RARA_ROBOT FOREIGN KEY (RobotID) REFERENCES ROBOT(RobotID) ON DELETE CASCADE,
    CONSTRAINT FK_RARA_AD_ROUTE FOREIGN KEY (AdRouteID) REFERENCES AD_ROUTE(AdRouteID) ON DELETE CASCADE
);

CREATE INDEX IX_RARA_RobotID ON ROBOT_AD_ROUTE_ASSIGNMENT(RobotID);
CREATE INDEX IX_RARA_AdRouteID ON ROBOT_AD_ROUTE_ASSIGNMENT(AdRouteID);
```

---

## 4. EF Core Mapping Changes

**File:** `Infrastructure/Persistence/AppDbContext.cs`

### 4.1 Update `AdRoute` configuration

```csharp
modelBuilder.Entity<AdRoute>(entity =>
{
    entity.ToTable("AD_ROUTE");
    entity.HasKey(x => x.AdRouteId);
    entity.Property(x => x.AdRouteId).HasColumnName("AdRouteID");
    entity.Property(x => x.RouteName).HasColumnName("RouteName").HasMaxLength(100).IsRequired();
    entity.Property(x => x.Description).HasColumnName("Description").HasMaxLength(500);
    entity.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
    entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");

    // === NEW ===
    entity.Property(x => x.IsAutonomous).HasColumnName("IsAutonomous").HasDefaultValue(false);
    entity.Property(x => x.SemanticObjectId).HasColumnName("SemanticObjectID");

    entity.HasOne(x => x.SemanticObject)
        .WithMany()
        .HasForeignKey(x => x.SemanticObjectId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasIndex(x => x.IsAutonomous);
});
```

### 4.2 Update `AdRouteNode` configuration

```csharp
modelBuilder.Entity<AdRouteNode>(entity =>
{
    // ... existing config ...
    entity.Property(x => x.ZoneId).HasColumnName("ZoneID");

    entity.HasOne(x => x.Zone)
        .WithMany()
        .HasForeignKey(x => x.ZoneId)
        .OnDelete(DeleteBehavior.SetNull);
});
```

### 4.3 Add `RobotAdRouteAssignment` configuration

```csharp
modelBuilder.Entity<RobotAdRouteAssignment>(entity =>
{
    entity.ToTable("ROBOT_AD_ROUTE_ASSIGNMENT");
    entity.HasKey(x => x.AssignmentId);
    entity.Property(x => x.AssignmentId).HasColumnName("AssignmentID");
    entity.Property(x => x.RobotId).HasColumnName("RobotID");
    entity.Property(x => x.AdRouteId).HasColumnName("AdRouteID");
    entity.Property(x => x.AssignedAt).HasColumnName("AssignedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
    entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).HasDefaultValue("Active");

    entity.HasOne(x => x.Robot)
        .WithMany()
        .HasForeignKey(x => x.RobotId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(x => x.AdRoute)
        .WithMany(r => r.Assignments)
        .HasForeignKey(x => x.AdRouteId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(x => x.RobotId);
    entity.HasIndex(x => x.AdRouteId);
});
```

---

## 5. DTO Changes

### 5.1 Update `AdRouteResponseDto`

**File:** `Application/Models/Ads/AdRouteDtos.cs`

```csharp
public sealed record AdRouteResponseDto(
    int AdRouteId,
    string RouteName,
    string? Description,
    bool IsActive,
    bool IsAutonomous,           // NEW
    int? SemanticObjectId,        // NEW
    string? SemanticObjectLabel,  // NEW: for display
    DateTime CreatedAt,
    List<AdRouteNodeDto> Nodes,
    List<int> CampaignIds);

public sealed record AdRouteNodeDto(
    int AdRouteNodeId,
    int NodeId,
    string? NodeName,
    int SequenceOrder,
    int DwellTimeSeconds,
    int? ZoneId,        // NEW
    string? ZoneName);  // NEW
```

### 5.2 Update Create/Update DTOs

```csharp
public sealed record CreateAdRouteRequestDto
{
    [Required(ErrorMessage = "RouteName không được để trống.")]
    [MaxLength(100)]
    public required string RouteName { get; init; }

    public string? Description { get; init; }

    // === NEW ===
    public bool IsAutonomous { get; init; } = false;
    public int? SemanticObjectId { get; init; }

    [Required]
    [MinLength(1)]
    public required List<AdRouteNodeInput> Nodes { get; init; }

    public List<int>? CampaignIds { get; init; }
}

public sealed record AdRouteNodeInput
{
    [Required]
    public required int NodeId { get; init; }

    public int SequenceOrder { get; init; }
    public int DwellTimeSeconds { get; init; } = 30;
    public int? ZoneId { get; init; }  // NEW
}
```

---

## 6. Runtime Logic: Ad Broadcast Compilation

### 6.1 Service: `IAdBroadcastService`

**File:** `Application/Interfaces/IAdBroadcastService.cs`

```csharp
public interface IAdBroadcastService
{
    /// <summary>
    /// Get current ad playlist based on robot position.
    /// Automatically detects mode (Autonomous vs Zone/Shelf).
    /// </summary>
    Task<AdPlaylistDto> GetPlaylistForRobotAsync(
        int robotId,
        int x, int y,
        CancellationToken ct = default);

    /// <summary>
    /// Get full autonomous route with pre-compiled playlists per stop.
    /// Called once at route start.
    /// </summary>
    Task<AutonomousRoutePlaylistDto?> GetAutonomousRoutePlaylistAsync(
        int robotId,
        CancellationToken ct = default);

    /// <summary>
    /// Get ads for Zone/Shelf mode based on spatial position.
    /// </summary>
    Task<AdPlaylistDto?> GetZoneShelfPlaylistAsync(
        int robotId,
        int x, int y,
        CancellationToken ct = default);
}
```

### 6.2 AdPlaylistDto

```csharp
public sealed record AdPlaylistDto(
    int RobotId,
    string Mode,           // "Autonomous" | "ZoneShelf" | "None"
    List<AdResourceDto> Resources,
    DateTime GeneratedAt);

public sealed record AdResourceDto(
    int ResourceId,
    string ResourceType,   // "image" | "video" | "text"
    string ResourceUrl,
    string? ContentText,
    string? Resolution,
    int Priority);

public sealed record AutonomousRoutePlaylistDto(
    int RobotId,
    int AdRouteId,
    string RouteName,
    List<AutonomousStopDto> Stops,
    DateTime GeneratedAt);

public sealed record AutonomousStopDto(
    int StopIndex,
    int NodeId,
    string? NodeName,
    int SequenceOrder,
    int DwellTimeSeconds,
    int? ZoneId,
    string? ZoneName,
    AdPlaylistDto Playlist);  // Pre-compiled playlist for this stop
```

### 6.3 Mode Detection Logic

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        AdPlaylist Compilation Flow                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Robot.position(x, y)                                                        │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐        │
│  │ 1. Get Robot's Active AdRouteAssignment                          │        │
│  └─────────────────────────────────────────────────────────────────┘        │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐        │
│  │ 2. Load AdRoute with Campaigns + Resources                      │        │
│  └─────────────────────────────────────────────────────────────────┘        │
│       │                                                                      │
│       ▼                                                                      │
│  ┌───────────────────────┐                                                  │
│  │ IsAutonomous == true? │                                                  │
│  └───────────┬───────────┘                                                  │
│              │                                                              │
│     ┌────────┴────────┐                                                      │
│     │ YES             │ NO                                                  │
│     ▼                 ▼                                                     │
│  ┌──────────────┐  ┌──────────────────────────────────────────┐            │
│  │ AUTONOMOUS   │  │ ZONE/SHELF MODE                           │            │
│  │ MODE         │  │                                          │            │
│  │              │  │ 1. Find SemanticObject where:             │            │
│  │ 1. Find      │  │    XMin ≤ x ≤ XMax AND                   │            │
│  │    current   │  │    YMin ≤ y ≤ YMax                       │            │
│  │    stop      │  │                                          │            │
│  │    by node   │  │ 2. If found AND                          │            │
│  │              │  │    AdCampaign.SemanticObjectId matches:   │            │
│  │ 2. Get       │  │    → broadcast AdCampaign's resources    │            │
│  │    playlist  │  │                                          │            │
│  │    for that  │  │ 3. If not in any AABB:                    │            │
│  │    stop      │  │    → return empty playlist               │            │
│  │              │  │                                          │            │
│  └──────────────┘  └──────────────────────────────────────────┘            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.4 Implementation: `AdBroadcastService`

**File:** `Infrastructure/Services/AdBroadcastService.cs`

```csharp
public sealed class AdBroadcastService : IAdBroadcastService
{
    public async Task<AdPlaylistDto> GetPlaylistForRobotAsync(
        int robotId, int x, int y, CancellationToken ct)
    {
        // 1. Get active AdRoute for robot
        var assignment = await GetActiveAssignmentAsync(robotId, ct);
        if (assignment == null)
            return EmptyPlaylist(robotId, "None");

        var adRoute = await LoadAdRouteWithCampaignsAsync(assignment.AdRouteId, ct);
        if (adRoute == null || !adRoute.IsActive)
            return EmptyPlaylist(robotId, "None");

        // 2. Route to appropriate mode
        if (adRoute.IsAutonomous)
            return await GetAutonomousPlaylistAsync(robotId, adRoute, x, y, ct);
        else
            return await GetZoneShelfPlaylistAsync(robotId, adRoute, x, y, ct);
    }

    public async Task<AdPlaylistDto> GetZoneShelfPlaylistAsync(
        int robotId, AdRoute adRoute, int x, int y, CancellationToken ct)
    {
        // Find SemanticObject containing robot position (AABB check)
        var semanticObject = await db.SemanticObjects
            .AsNoTracking()
            .Where(so => so.XMin <= x && x <= so.XMax
                      && so.YMin <= y && y <= so.YMax)
            .FirstOrDefaultAsync(ct);

        if (semanticObject == null)
            return EmptyPlaylist(robotId, "ZoneShelf");

        // Find active campaign targeting this SemanticObject
        var now = DateTime.UtcNow;
        var campaign = await db.AdCampaigns
            .AsNoTracking()
            .Where(c => c.SemanticObjectId == semanticObject.ObjectId
                     && c.Status == CampaignStatus.Active
                     && c.StartDate <= now && c.EndDate >= now)
            .Include(c => c.AdResources.Where(r => r.Status == "Active"))
            .FirstOrDefaultAsync(ct);

        if (campaign == null)
            return EmptyPlaylist(robotId, "ZoneShelf");

        // Also check AdRoute.Campaigns (explicit linking)
        if (!adRoute.Campaigns.Any(c => c.AdCampaignId == campaign.AdCampaignId))
        {
            // Campaign exists but not linked to this AdRoute
            return EmptyPlaylist(robotId, "ZoneShelf");
        }

        return BuildPlaylist(robotId, "ZoneShelf", campaign.AdResources);
    }

    public async Task<AdPlaylistDto> GetAutonomousPlaylistAsync(
        int robotId, AdRoute adRoute, int x, int y, CancellationToken ct)
    {
        // 1. Find current stop by nearest node
        var currentNode = await db.NavigationNodes
            .AsNoTracking()
            .Where(n => n.XCoord == x && n.YCoord == y)
            .FirstOrDefaultAsync(ct);

        if (currentNode == null)
            return EmptyPlaylist(robotId, "Autonomous");

        // 2. Find which AdRouteNode this corresponds to
        var routeNode = adRoute.Nodes
            .Where(n => n.NodeId == currentNode.NodeId)
            .OrderBy(n => n.SequenceOrder)
            .FirstOrDefault();

        if (routeNode == null)
            return EmptyPlaylist(robotId, "Autonomous");

        // 3. Get Zone's campaigns for this stop
        if (!routeNode.ZoneId.HasValue)
            return EmptyPlaylist(robotId, "Autonomous");

        var now = DateTime.UtcNow;
        var campaigns = await db.AdCampaignZones
            .AsNoTracking()
            .Where(cz => cz.ZoneId == routeNode.ZoneId.Value)
            .Where(cz => cz.AdCampaign.Status == CampaignStatus.Active
                      && cz.AdCampaign.StartDate <= now
                      && cz.AdCampaign.EndDate >= now)
            .Include(cz => cz.AdCampaign.AdResources.Where(r => r.Status == "Active"))
            .ToListAsync(ct);

        var resources = campaigns
            .SelectMany(cz => cz.AdCampaign.AdResources)
            .OrderBy(r => r.ResourceType)  // Or by Priority
            .ToList();

        return BuildPlaylist(robotId, "Autonomous", resources);
    }
}
```

### 6.5 Autonomous Route Pre-compilation

Called once when robot starts an autonomous route:

```csharp
public async Task<AutonomousRoutePlaylistDto?> GetAutonomousRoutePlaylistAsync(
    int robotId, CancellationToken ct)
{
    var assignment = await GetActiveAssignmentAsync(robotId, ct);
    if (assignment == null) return null;

    var adRoute = await db.AdRoutes
        .AsNoTracking()
        .Include(r => r.Nodes.OrderBy(n => n.SequenceOrder))
            .ThenInclude(n => n.Node)
        .Include(r => r.Nodes.Where(n => n.ZoneId != null))
            .ThenInclude(n => n.Zone)
        .Include(r => r.Campaigns)
        .FirstOrDefaultAsync(r => r.AdRouteId == assignment.AdRouteId, ct);

    if (adRoute == null || !adRoute.IsAutonomous) return null;

    var now = DateTime.UtcNow;
    var zoneIds = adRoute.Nodes
        .Where(n => n.ZoneId.HasValue)
        .Select(n => n.ZoneId.Value)
        .Distinct()
        .ToList();

    // Batch load all zone campaigns
    var zoneCampaigns = await db.AdCampaignZones
        .AsNoTracking()
        .Where(cz => zoneIds.Contains(cz.ZoneId))
        .Where(cz => cz.AdCampaign.Status == CampaignStatus.Active
                  && cz.AdCampaign.StartDate <= now
                  && cz.AdCampaign.EndDate >= now)
        .Include(cz => cz.AdCampaign.AdResources.Where(r => r.Status == "Active"))
        .ToListAsync(ct);

    var playlistsByZone = zoneCampaigns
        .GroupBy(cz => cz.ZoneId)
        .ToDictionary(
            g => g.Key,
            g => BuildPlaylist(robotId, "Autonomous",
                g.SelectMany(cz => cz.AdCampaign.AdResources)
                 .OrderBy(r => r.ResourceType)
                 .ToList()));

    // Build stops with pre-compiled playlists
    var stops = adRoute.Nodes.Select(n => new AutonomousStopDto(
        n.SequenceOrder,
        n.NodeId,
        n.Node?.NodeName,
        n.SequenceOrder,
        n.DwellTimeSeconds,
        n.ZoneId,
        n.Zone?.ZoneName,
        playlistsByZone.GetValueOrDefault(n.ZoneId ?? 0)
            ?? EmptyPlaylist(robotId, "Autonomous")
    )).ToList();

    return new AutonomousRoutePlaylistDto(
        robotId,
        adRoute.AdRouteId,
        adRoute.RouteName,
        stops,
        DateTime.UtcNow);
}
```

---

## 7. API Endpoints

### 7.1 Existing (keep)

- `GET /api/v1/adroutes` - List AdRoutes
- `GET /api/v1/adroutes/{id}` - Get AdRoute detail
- `POST /api/v1/adroutes` - Create AdRoute
- `PUT /api/v1/adroutes/{id}` - Update AdRoute
- `DELETE /api/v1/adroutes/{id}` - Delete AdRoute
- `POST /api/v1/adroutes/{id}/assign/{robotId}` - Assign to robot

### 7.2 New Endpoints

```
POST /api/v1/adroutes/{id}/activate
    → Set IsActive = true, create RobotAdRouteAssignment

POST /api/v1/adroutes/{id}/deactivate
    → Set IsActive = false, update assignment Status = "Completed"

GET /api/v1/adroutes/{id}/playlist
    → Get pre-compiled autonomous playlist for this route

GET /api/v1/robots/{robotId}/playlist?x={x}&y={y}
    → Get current playlist based on robot position
```

### 7.3 Robot IoT Endpoints (for Android)

```
GET /api/v1/robot/{robotCode}/broadcast/now
    → Returns current AdPlaylistDto based on robot's last known position
    → Called by robot periodically (every 5-10 seconds)

GET /api/v1/robot/{robotCode}/broadcast/route
    → Returns full AutonomousRoutePlaylistDto for autonomous mode
    → Called once when robot starts autonomous route
```

---

## 8. Implied Changes Summary

### Files to CREATE
| File | Purpose |
|------|---------|
| `Domain/Entities/RobotAdRouteAssignment.cs` | New entity for robot-adroute linking |
| `Application/Interfaces/IAdBroadcastService.cs` | Interface for ad compilation |
| `Infrastructure/Services/AdBroadcastService.cs` | Implementation of ad broadcast logic |
| `Infrastructure/Services/RobotAdRouteService.cs` | CRUD for robot-adroute assignments |

### Files to MODIFY
| File | Changes |
|------|---------|
| `Domain/Entities/AdRoute.cs` | Add `IsAutonomous`, `SemanticObjectId` |
| `Domain/Entities/AdRouteNode.cs` | Add `ZoneId` |
| `Application/Models/Ads/AdRouteDtos.cs` | Add new DTO fields |
| `Application/Interfaces/IAdRouteService.cs` | Update interface |
| `Infrastructure/Services/AdRouteService.cs` | Update CRUD, add assignment methods |
| `Infrastructure/Persistence/AppDbContext.cs` | Add mappings, remove old converter |
| `db/ddl_erd.sql` | Add new columns and table |

### Files to MIGRATE (Database)
```bash
dotnet ef migrations add AddAdRouteModeAndAssignment \
    --project src/SmartMarketBot.Infrastructure \
    --startup-project src/SmartMarketBot.API
```

---

## 9. Implementation Order

```
PHASE 1: Schema & Entity (Foundation)
├── 1.1 Create RobotAdRouteAssignment entity
├── 1.2 Add IsAutonomous, SemanticObjectId to AdRoute
├── 1.3 Add ZoneId to AdRouteNode
├── 1.4 Update EF Core mappings
├── 1.5 Create & run migration
└── 1.6 Update DTOs

PHASE 2: Service Layer
├── 2.1 Create IAdBroadcastService interface
├── 2.2 Implement AdBroadcastService
│   ├── Zone/Shelf mode (AABB)
│   └── Autonomous mode (sequential)
├── 2.3 Update AdRouteService for new fields
└── 2.4 Create RobotAdRouteService

PHASE 3: API Endpoints
├── 3.1 Add /playlist endpoints to AdRoutesController
├── 3.2 Add /broadcast endpoints to RobotController
└── 3.3 Update AdRouteDtos for responses

PHASE 4: Testing & Integration
├── 4.1 Test Zone/Shelf mode with AABB
├── 4.2 Test Autonomous mode with playlist
├── 4.3 Test robot assignment workflow
└── 4.4 Update FE if needed
```

---

## 10. Edge Cases

### 10.1 Robot in no SemanticObject AABB (Zone/Shelf mode)
```csharp
// Return empty playlist, robot doesn't broadcast
return EmptyPlaylist(robotId, "ZoneShelf");
```

### 10.2 Autonomous route has no ZoneId on nodes
```csharp
// Skip playlist compilation for that stop
// Or use AdRoute's default campaigns
```

### 10.3 Campaign has no active resources
```csharp
// Return empty playlist, log warning
logger.LogWarning("Campaign {Id} has no active resources", campaign.AdCampaignId);
```

### 10.4 Multiple SemanticObjects overlap
```csharp
// Take the first match (or most specific/largest confidence)
// Prefer exact object type match
```

### 10.5 Robot switches from Patrol to Ad route
```csharp
// IoT endpoint checks RouteAssignment vs RobotAdRouteAssignment
// Two parallel assignment systems, robot handles priority
```

---

## 11. Security & Performance

### Security
- Robot can only access its own broadcasts (`robotCode` auth)
- AdResources filtered by `Status == "Active"` and campaign date range
- Rate limiting on broadcast endpoints (5-10 second intervals)

### Performance
- Pre-compile autonomous playlists at route start (not per-request)
- Batch load campaigns by zoneIds (avoid N+1)
- Cache SemanticObjects AABB data in memory for fast lookup
- Use spatial index if available (not required for small stores)

---

## 12. Future Enhancements (Out of Scope)

- [ ] Machine learning for optimal ad timing
- [ ] Real-time bid competition between campaigns
- [ ] Customer attention tracking (how long they view ads)
- [ ] A/B testing for ad placements
- [ ] Geo-fencing with dynamic AABB (moving zones)
