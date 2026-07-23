-- ============================================================
-- SmartMarketBot — SQL Script Đè Map Lưới 18 Nodes (Dense Grid)
-- ============================================================

USE SuperMarketBot;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Xóa dữ liệu Map cũ liên quan đến MapID 1 hoặc FloorID 1
DELETE FROM dbo.NAVIGATION_EDGE WHERE FromNodeId IN (SELECT NodeId FROM dbo.NAVIGATION_NODE WHERE MapID = 1) OR ToNodeId IN (SELECT NodeId FROM dbo.NAVIGATION_NODE WHERE MapID = 1);
DELETE FROM dbo.AISLE_NODE WHERE NodeId IN (SELECT NodeId FROM dbo.NAVIGATION_NODE WHERE MapID = 1);
DELETE FROM dbo.ROUTE_NODE_MAPPING WHERE NodeId IN (SELECT NodeId FROM dbo.NAVIGATION_NODE WHERE MapID = 1);
DELETE FROM dbo.NAVIGATION_NODE WHERE MapID = 1;
DELETE FROM dbo.SEMANTIC_OBJECT WHERE MapID = 1;
DELETE FROM dbo.ROBOT_ROUTE WHERE MapID = 1;
DELETE FROM dbo.MAP WHERE MapID = 1 OR FloorID = 1;
GO

-- 2. Thêm MAP mới (Floor 1, 3m x 3m)
SET IDENTITY_INSERT dbo.MAP ON;
INSERT INTO dbo.MAP (MapID, FloorID, MapName, MapData, FloorplanImageUrl, WidthMeters, HeightMeters, CreatedAt) VALUES
(1, 1, N'Supermarket_3x3m_Grid', N'{"floorId":1,"mapName":"Supermarket_3x3m_Grid","widthMeters":3.0,"heightMeters":3.0}', NULL, 3.0, 3.0, GETUTCDATE());
SET IDENTITY_INSERT dbo.MAP OFF;
GO

-- 3. Thêm NAVIGATION_NODE (18 grid nodes)
SET IDENTITY_INSERT dbo.NAVIGATION_NODE ON;
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (1, 1, N'Node_0.25_0.2', 0.25, 0.2, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (2, 1, N'Node_1.5_0.2', 1.5, 0.2, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (3, 1, N'Node_2.75_0.2', 2.75, 0.2, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (4, 1, N'Node_0.25_0.6', 0.25, 0.6, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (5, 1, N'Node_1.5_0.6', 1.5, 0.6, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (6, 1, N'Node_2.75_0.6', 2.75, 0.6, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (7, 1, N'Node_0.25_1.1', 0.25, 1.1, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (8, 1, N'Node_1.5_1.1', 1.5, 1.1, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (9, 1, N'Node_2.75_1.1', 2.75, 1.1, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (10, 1, N'Node_0.25_1.45', 0.25, 1.45, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (11, 1, N'Node_1.5_1.45', 1.5, 1.45, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (12, 1, N'Node_2.75_1.45', 2.75, 1.45, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (13, 1, N'Node_0.25_2.1', 0.25, 2.1, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (14, 1, N'Node_1.5_2.1', 1.5, 2.1, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (15, 1, N'Node_2.75_2.1', 2.75, 2.1, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (16, 1, N'Node_0.25_2.8', 0.25, 2.8, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (17, 1, N'Node_1.5_2.8', 1.5, 2.8, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (18, 1, N'Node_2.75_2.8', 2.75, 2.8, N'Corridor', 0);
SET IDENTITY_INSERT dbo.NAVIGATION_NODE OFF;
GO

-- 4. Thêm NAVIGATION_EDGE
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (1, 2, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (2, 1, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (2, 3, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (3, 2, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (4, 5, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (5, 4, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (5, 6, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (6, 5, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (7, 8, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (8, 7, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (8, 9, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (9, 8, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (10, 11, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (11, 10, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (11, 12, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (12, 11, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (13, 14, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (14, 13, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (14, 15, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (15, 14, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (16, 17, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (17, 16, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (17, 18, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (18, 17, 1.25, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (1, 4, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (4, 1, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (4, 7, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (7, 4, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (7, 10, 0.35, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (10, 7, 0.35, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (10, 13, 0.65, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (13, 10, 0.65, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (13, 16, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (16, 13, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (2, 5, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (5, 2, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (5, 8, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (8, 5, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (8, 11, 0.35, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (11, 8, 0.35, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (11, 14, 0.65, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (14, 11, 0.65, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (14, 17, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (17, 14, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (3, 6, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (6, 3, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (6, 9, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (9, 6, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (9, 12, 0.35, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (12, 9, 0.35, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (12, 15, 0.65, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (15, 12, 0.65, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (15, 18, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (18, 15, 0.7, 1);
GO

-- 5. Ánh xạ Kệ Hàng vào Node (AISLE_NODE)
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (1, 14);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (2, 15);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (3, 13);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (4, 14);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (5, 10);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (6, 12);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (7, 7);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (8, 9);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (9, 8);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (10, 8);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (11, 5);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (12, 5);
GO

PRINT '✅ Cập nhật Map 18-Node Grid vào SQL Server thành công!';
