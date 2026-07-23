-- ============================================================
-- SmartMarketBot — SQL Script Đè Map Lưới Đi Xuyên Kệ (In-Aisle)
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
(1, 1, N'Supermarket_3x3m_InAisle', N'{"floorId":1,"mapName":"Supermarket_3x3m_InAisle","widthMeters":3.0,"heightMeters":3.0}', NULL, 3.0, 3.0, GETUTCDATE());
SET IDENTITY_INSERT dbo.MAP OFF;
GO

-- 3. Thêm NAVIGATION_NODE (13 in-aisle nodes)
SET IDENTITY_INSERT dbo.NAVIGATION_NODE ON;
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (1, 1, N'Start (Cửa)', 1.9, 0.2, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (2, 1, N'Quầy Thu Ngân (Checkout)', 1.3, 0.2, N'CHECKOUT', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (3, 1, N'Giao lộ Dưới', 1.5, 0.2, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (4, 1, N'Giao lộ Giữa', 1.5, 1.0, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (5, 1, N'Giao lộ Trung tâm', 1.5, 1.5, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (6, 1, N'Lối vào Kệ Xanh Lá', 0.8, 1.5, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (7, 1, N'Lối vào Kệ Xanh Dương', 2.2, 1.5, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (8, 1, N'Trong Kệ Xanh Lá (Dưới)', 0.8, 1.8, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (9, 1, N'Trong Kệ Xanh Lá (Trên)', 0.8, 2.4, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (10, 1, N'Trong Kệ Xanh Dương (Dưới)', 2.2, 1.8, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (11, 1, N'Trong Kệ Xanh Dương (Trên)', 2.2, 2.4, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (12, 1, N'Trong Kệ Vàng (Trái)', 1.8, 1.0, N'Corridor', 0);
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES (13, 1, N'Trong Kệ Vàng (Phải)', 2.4, 1.0, N'Corridor', 0);
SET IDENTITY_INSERT dbo.NAVIGATION_NODE OFF;
GO

-- 4. Thêm NAVIGATION_EDGE
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (1, 3, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (3, 1, 0.4, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (2, 3, 0.2, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (3, 2, 0.2, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (3, 4, 0.8, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (4, 3, 0.8, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (4, 5, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (5, 4, 0.5, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (4, 12, 0.3, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (12, 4, 0.3, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (12, 13, 0.6, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (13, 12, 0.6, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (5, 6, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (6, 5, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (5, 7, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (7, 5, 0.7, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (6, 8, 0.3, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (8, 6, 0.3, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (8, 9, 0.6, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (9, 8, 0.6, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (7, 10, 0.3, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (10, 7, 0.3, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (10, 11, 0.6, 1);
INSERT INTO dbo.NAVIGATION_EDGE (FromNodeId, ToNodeId, Distance, IsBidirectional) VALUES (11, 10, 0.6, 1);
GO

-- 5. Ánh xạ Kệ Hàng vào Node (AISLE_NODE)
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (1, 8);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (2, 9);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (3, 10);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (4, 11);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (10, 13);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (5, 8);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (6, 9);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (7, 10);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (8, 11);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (9, 12);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (11, 12);
INSERT INTO dbo.AISLE_NODE (AisleID, NodeID) VALUES (12, 13);
GO

PRINT '✅ Cập nhật Map 13-Node In-Aisle vào SQL Server thành công!';
