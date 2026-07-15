-- Chạy MỘT LẦN trong SSMS để tạo mới Database trống
USE master;
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'SuperMarketBotDb')
    DROP DATABASE SuperMarketBotDb;
CREATE DATABASE SuperMarketBotDb;
GO

-- Cách thiết lập dữ liệu mẫu:
-- 1. Chạy file 'erd_database.sql' để sinh cấu trúc 37 bảng.
-- 2. Chạy file 'seed_map_navigation.sql' để nạp sơ đồ bản đồ & robot.
-- 3. Chạy file 'seed_data_dev.sql' để nạp toàn bộ sản phẩm, thành viên và giao dịch mẫu.

