-- Chạy MỘT LẦN trong SSMS
USE master;
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'SuperMarketBotDb')
    DROP DATABASE SuperMarketBotDb;
CREATE DATABASE SuperMarketBotDb;
GO

-- Chạy erd_database.sql vào SuperMarketBotDb (F5)
-- Chạy seed_erd_v4.sql vào SuperMarketBotDb (F5)
