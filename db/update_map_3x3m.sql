-- ============================================================
-- SmartMarketBot — SQL Script Đè Map Test 3m x 3m vào Database
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
(1, 1, N'Supermarket_3x3m_Exported', N'{"floorId":1,"mapName":"Supermarket_3x3m_Exported","widthMeters":3.0,"heightMeters":3.0}', NULL, 3.0, 3.0, GETUTCDATE());
SET IDENTITY_INSERT dbo.MAP OFF;
GO

-- 3. Thêm NAVIGATION_NODE (8 node tọa độ)
-- Phase B: thêm cột NodeCode — định danh vật lý trên line-scanning.
-- Mã code theo pattern L{Map}_{Stop} ví dụ "L1-S1" cho start,
-- "L1-KV1-1" cho stop trước kệ KV1 row 1, v.v.
SET IDENTITY_INSERT dbo.NAVIGATION_NODE ON;
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeCode, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES
(1, 1, N'L1-S1',  N'START_NODE',     0.500, 0.200, N'start',       0),
(2, 1, N'L1-K1A', N'NODE_KV1_Shelf_1', 0.600, 2.100, N'destination', 0),
(3, 1, N'L1-K1B', N'NODE_KV1_Shelf_2', 1.000, 2.100, N'destination', 0),
(4, 1, N'L1-K2A', N'NODE_KV2_Shelf_1', 2.000, 2.100, N'destination', 0),
(5, 1, N'L1-K2B', N'NODE_KV2_Shelf_2', 2.400, 2.100, N'destination', 0),
(6, 1, N'L1-K3A', N'NODE_KV3_Shelf_1', 2.200, 1.100, N'destination', 0),
(7, 1, N'L1-K3B', N'NODE_KV3_Shelf_2', 2.200, 0.600, N'destination', 0),
(8, 1, N'L1-CO',  N'CHECKOUT_NODE',  2.500, 0.200, N'checkout',    0);
SET IDENTITY_INSERT dbo.NAVIGATION_NODE OFF;
GO

-- 4. Thêm NAVIGATION_EDGE (Các đường nối)
SET IDENTITY_INSERT dbo.NAVIGATION_EDGE ON;
INSERT INTO dbo.NAVIGATION_EDGE (EdgeID, FromNodeID, ToNodeID, Distance, IsBidirectional) VALUES
(1, 1, 2, 1.90, 1),
(2, 2, 3, 0.40, 1),
(3, 3, 4, 1.00, 1),
(4, 4, 5, 0.40, 1),
(5, 5, 6, 1.00, 1),
(6, 6, 7, 0.50, 1),
(7, 7, 8, 0.50, 1);
SET IDENTITY_INSERT dbo.NAVIGATION_EDGE OFF;
GO

-- 5. Thêm SEMANTIC_OBJECT (6 kệ hàng + 1 quầy checkout)
SET IDENTITY_INSERT dbo.SEMANTIC_OBJECT ON;
INSERT INTO dbo.SEMANTIC_OBJECT (ObjectID, MapID, ObjectType, XMin, YMin, XMax, YMax, Label, Confidence, DetectedAt, ImageUrl, ProductTypeID) VALUES
(1, 1, N'shelf', 0.475, 1.600, 0.725, 2.600, N'KV1_Shelf_1', 1.0, GETUTCDATE(), NULL, NULL),
(2, 1, N'shelf', 0.875, 1.600, 1.125, 2.600, N'KV1_Shelf_2', 1.0, GETUTCDATE(), NULL, NULL),
(3, 1, N'shelf', 1.875, 1.600, 2.125, 2.600, N'KV2_Shelf_1', 1.0, GETUTCDATE(), NULL, NULL),
(4, 1, N'shelf', 2.275, 1.600, 2.525, 2.600, N'KV2_Shelf_2', 1.0, GETUTCDATE(), NULL, NULL),
(5, 1, N'shelf', 1.700, 0.975, 2.700, 1.225, N'KV3_Shelf_1', 1.0, GETUTCDATE(), NULL, NULL),
(6, 1, N'shelf', 1.700, 0.475, 2.700, 0.725, N'KV3_Shelf_2', 1.0, GETUTCDATE(), NULL, NULL),
(7, 1, N'checkout', 2.300, 0.050, 2.700, 0.350, N'Checkout Desk', 1.0, GETUTCDATE(), NULL, NULL);
SET IDENTITY_INSERT dbo.SEMANTIC_OBJECT OFF;
GO

PRINT '✅ Cập nhật Map 3m x 3m mới vào SQL Server thành công!';
GO
