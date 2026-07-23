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

-- 3. Thêm NAVIGATION_NODE (13 node: tất cả nằm trên hành lang lối đi, KHÔNG ĐÂM VÀO KỆ)
SET IDENTITY_INSERT dbo.NAVIGATION_NODE ON;
INSERT INTO dbo.NAVIGATION_NODE (NodeID, MapID, NodeName, XCoord, YCoord, NodeType, IsBlocked) VALUES
(1, 1, N'START_NODE', 0.500, 0.200, N'start', 0),
(2, 1, N'NODE_KV1_Shelf_1', 0.600, 1.450, N'destination', 0),  -- Trước kệ KV1_1 (Y=1.45)
(3, 1, N'NODE_KV1_Shelf_2', 1.000, 1.450, N'destination', 0),  -- Trước kệ KV1_2 (Y=1.45)
(4, 1, N'NODE_KV2_Shelf_1', 2.000, 1.450, N'destination', 0),  -- Trước kệ KV2_1 (Y=1.45)
(5, 1, N'NODE_KV2_Shelf_2', 2.400, 1.450, N'destination', 0),  -- Trước kệ KV2_2 (Y=1.45)
(6, 1, N'NODE_KV3_Shelf_1', 1.450, 1.100, N'destination', 0),  -- Trước kệ KV3_1 (X=1.45)
(7, 1, N'NODE_KV3_Shelf_2', 1.450, 0.600, N'destination', 0),  -- Trước kệ KV3_2 (X=1.45)
(8, 1, N'CHECKOUT_NODE', 2.500, 0.200, N'checkout', 0),     -- Trước quầy thu ngân (Y=0.2)
-- Node giao lộ nối lưới hành lang
(9, 1, N'INTER_LEFT_MID', 0.500, 1.450, N'intersection', 0),
(10, 1, N'INTER_CENTER', 1.450, 1.450, N'intersection', 0),
(11, 1, N'INTER_RIGHT_MID', 2.400, 1.450, N'intersection', 0),
(12, 1, N'INTER_BOT_MID', 1.450, 0.200, N'intersection', 0),
(13, 1, N'INTER_BOT_RIGHT', 2.200, 0.200, N'intersection', 0);
SET IDENTITY_INSERT dbo.NAVIGATION_NODE OFF;
GO

-- 4. Thêm NAVIGATION_EDGE (100% Cạnh vuông góc 90 độ nằm trên hành lang mở)
SET IDENTITY_INSERT dbo.NAVIGATION_EDGE ON;
INSERT INTO dbo.NAVIGATION_EDGE (EdgeID, FromNodeID, ToNodeID, Distance, IsBidirectional) VALUES
-- Trục hành lang dọc bên trái (X=0.5)
(1, 1, 9, 1.25, 1),      -- START (0.5, 0.2) -> INTER_LEFT_MID (0.5, 1.45)
-- Trục hành lang ngang giữa (Y=1.45, phía trước kệ KV1 & KV2)
(2, 9, 2, 0.10, 1),      -- INTER_LEFT_MID (0.5, 1.45) -> KV1_S1 (0.6, 1.45)
(3, 2, 3, 0.40, 1),      -- KV1_S1 (0.6, 1.45) -> KV1_S2 (1.0, 1.45)
(4, 3, 10, 0.45, 1),     -- KV1_S2 (1.0, 1.45) -> INTER_CENTER (1.45, 1.45)
(5, 10, 4, 0.55, 1),     -- INTER_CENTER (1.45, 1.45) -> KV2_S1 (2.0, 1.45)
(6, 4, 5, 0.40, 1),      -- KV2_S1 (2.0, 1.45) -> KV2_S2 (2.4, 1.45)
(7, 5, 11, 0.10, 1),     -- KV2_S2 (2.4, 1.45) -> INTER_RIGHT_MID (2.4, 1.45)
-- Trục hành lang dọc giữa (X=1.45, phía bên trái kệ KV3)
(8, 10, 6, 0.35, 1),     -- INTER_CENTER (1.45, 1.45) -> KV3_S1 (1.45, 1.1)
(9, 6, 7, 0.50, 1),      -- KV3_S1 (1.45, 1.1) -> KV3_S2 (1.45, 0.6)
(10, 7, 12, 0.40, 1),    -- KV3_S2 (1.45, 0.6) -> INTER_BOT_MID (1.45, 0.2)
-- Trục hành lang ngang dưới (Y=0.20)
(11, 1, 12, 0.95, 1),    -- START (0.5, 0.2) -> INTER_BOT_MID (1.45, 0.2)
(12, 12, 13, 0.75, 1),   -- INTER_BOT_MID (1.45, 0.2) -> INTER_BOT_RIGHT (2.2, 0.2)
(13, 13, 8, 0.30, 1);    -- INTER_BOT_RIGHT (2.2, 0.2) -> CHECKOUT (2.5, 0.2)
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

PRINT '✅ Cập nhật Map 3m x 3m mới (Orthogonal 90 degree) vào SQL Server thành công!';
GO
