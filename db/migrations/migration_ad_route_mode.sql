-- Migration: Add AdRoute Mode + RobotAdRouteAssignment (UPPER_CASE for Azure)
-- Created: 2026-07-22
-- Aligns with ADR-ROUTE-INTEGRATION-PLAN.md
-- Target DB: schema dùng snake_upper_case (per db/erd_database.sql)

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. AD_ROUTE table (create if missing - schema dùng EF Core có thể đã tạo)
IF OBJECT_ID(N'[dbo].[AD_ROUTE]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AD_ROUTE] (
        [AdRouteID]    INT IDENTITY(1,1) PRIMARY KEY,
        [RouteName]    NVARCHAR(100) NOT NULL,
        [Description]  NVARCHAR(500) NULL,
        [IsActive]     BIT NOT NULL DEFAULT 1,
        [IsAutonomous] BIT NOT NULL DEFAULT 0,
        [SemanticObjectID] INT NULL,
        [CreatedAt]    DATETIME NOT NULL CONSTRAINT [DF_AD_ROUTE_CreatedAt] DEFAULT (DATEADD(hour, 7, GETUTCDATE()))
    );
END;

-- Add columns if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AD_ROUTE]') AND name = 'IsAutonomous')
    ALTER TABLE [dbo].[AD_ROUTE] ADD [IsAutonomous] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AD_ROUTE]') AND name = 'SemanticObjectID')
    ALTER TABLE [dbo].[AD_ROUTE] ADD [SemanticObjectID] INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AD_ROUTE_SEMANTIC_OBJECT')
    ALTER TABLE [dbo].[AD_ROUTE]
        ADD CONSTRAINT [FK_AD_ROUTE_SEMANTIC_OBJECT]
        FOREIGN KEY ([SemanticObjectID]) REFERENCES [dbo].[SEMANTIC_OBJECT]([ObjectID])
        ON DELETE SET NULL;

-- 2. AD_ROUTE_NODE
IF OBJECT_ID(N'[dbo].[AD_ROUTE_NODE]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AD_ROUTE_NODE] (
        [AdRouteNodeID]   INT IDENTITY(1,1) PRIMARY KEY,
        [AdRouteID]       INT NOT NULL,
        [NodeID]          INT NOT NULL,
        [SequenceOrder]   INT NOT NULL DEFAULT 0,
        [DwellTimeSeconds] INT NOT NULL DEFAULT 30,
        [ZoneID]          INT NULL,
        CONSTRAINT [FK_AD_ROUTE_NODE_AD_ROUTE] FOREIGN KEY ([AdRouteID])
            REFERENCES [dbo].[AD_ROUTE]([AdRouteID]) ON DELETE CASCADE,
        CONSTRAINT [FK_AD_ROUTE_NODE_NAV_NODE] FOREIGN KEY ([NodeID])
            REFERENCES [dbo].[NAVIGATION_NODE]([NodeID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AD_ROUTE_NODE]') AND name = 'ZoneID')
    ALTER TABLE [dbo].[AD_ROUTE_NODE] ADD [ZoneID] INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AD_ROUTE_NODE_ZONE')
    ALTER TABLE [dbo].[AD_ROUTE_NODE]
        ADD CONSTRAINT [FK_AD_ROUTE_NODE_ZONE]
        FOREIGN KEY ([ZoneID]) REFERENCES [dbo].[ZONE]([ZoneID])
        ON DELETE SET NULL;

-- 3. ROBOT_AD_ROUTE_ASSIGNMENT
IF OBJECT_ID(N'[dbo].[ROBOT_AD_ROUTE_ASSIGNMENT]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ROBOT_AD_ROUTE_ASSIGNMENT] (
        [AssignmentID] INT IDENTITY(1,1) PRIMARY KEY,
        [RobotID]      INT NOT NULL,
        [AdRouteID]    INT NOT NULL,
        [AssignedAt]   DATETIME NOT NULL CONSTRAINT [DF_RARA_AssignedAt] DEFAULT (DATEADD(hour, 7, GETUTCDATE())),
        [Status]       NVARCHAR(50) NOT NULL CONSTRAINT [DF_RARA_Status] DEFAULT (N'Active'),
        CONSTRAINT [FK_RARA_ROBOT] FOREIGN KEY ([RobotID])
            REFERENCES [dbo].[ROBOT]([RobotID]) ON DELETE CASCADE,
        CONSTRAINT [FK_RARA_AD_ROUTE] FOREIGN KEY ([AdRouteID])
            REFERENCES [dbo].[AD_ROUTE]([AdRouteID]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_RARA_RobotID]   ON [dbo].[ROBOT_AD_ROUTE_ASSIGNMENT]([RobotID]);
    CREATE INDEX [IX_RARA_AdRouteID] ON [dbo].[ROBOT_AD_ROUTE_ASSIGNMENT]([AdRouteID]);
END;

-- 4. AD_ROUTE_CAMPAIGN (link table nếu thiếu)
IF OBJECT_ID(N'[dbo].[AD_ROUTE_CAMPAIGN]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AD_ROUTE_CAMPAIGN] (
        [AdRouteID]   INT NOT NULL,
        [AdCampaignID] INT NOT NULL,
        CONSTRAINT [PK_AD_ROUTE_CAMPAIGN] PRIMARY KEY ([AdRouteID], [AdCampaignID]),
        CONSTRAINT [FK_ARC_AD_ROUTE] FOREIGN KEY ([AdRouteID])
            REFERENCES [dbo].[AD_ROUTE]([AdRouteID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ARC_AD_CAMPAIGN] FOREIGN KEY ([AdCampaignID])
            REFERENCES [dbo].[AD_CAMPAIGN]([AdCampaignID]) ON DELETE CASCADE
    );
END;

COMMIT TRANSACTION;
GO

PRINT 'Migration completed.';
