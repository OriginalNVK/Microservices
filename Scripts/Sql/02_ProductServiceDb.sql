-- ============================================
-- ProductService Database Migration Script
-- Database: ProductServiceDb
-- ============================================

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'ProductServiceDb')
BEGIN
    CREATE DATABASE ProductServiceDb;
END
GO

USE ProductServiceDb;
GO

-- ============================================
-- Table: Loai
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Loai')
BEGIN
    CREATE TABLE Loai
    (
        MaLoai      INT IDENTITY(1,1) PRIMARY KEY,
        TenLoai     NVARCHAR(50)  NOT NULL,
        TenLoaiAlias NVARCHAR(50) NULL,
        MoTa        NVARCHAR(MAX) NULL,
        Hinh        NVARCHAR(MAX) NULL
    );

    PRINT 'Table Loai created.';
END
ELSE
    PRINT 'Table Loai already exists.';
GO

-- ============================================
-- Table: HangHoa
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HangHoa')
BEGIN
    CREATE TABLE HangHoa
    (
        MaHH       INT IDENTITY(1,1) PRIMARY KEY,
        TenHH      NVARCHAR(100)   NOT NULL,
        TenAlias   NVARCHAR(100)   NULL,
        MaLoai     INT             NOT NULL,
        MoTaDonVi  NVARCHAR(50)    NULL,
        DonGia     DECIMAL(18,2)   NULL,
        Hinh       NVARCHAR(255)   NULL,
        NgaySX     DATE            NOT NULL,
        GiamGia    DECIMAL(5,2)    NOT NULL DEFAULT 0,
        LuotMua    INT             NOT NULL DEFAULT 0,
        MoTa       NVARCHAR(MAX)   NULL,

        CONSTRAINT FK_HangHoa_Loai
            FOREIGN KEY (MaLoai) REFERENCES Loai(MaLoai)
            ON DELETE RESTRICT
    );

    PRINT 'Table HangHoa created.';
END
ELSE
    PRINT 'Table HangHoa already exists.';
GO

-- ============================================
-- EF Core Migrations History Table
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory
    (
        MigrationId    NVARCHAR(150) NOT NULL PRIMARY KEY,
        ProductVersion NVARCHAR(32)  NOT NULL
    );

    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20240101000000_InitialCreate', '8.0.0');

    PRINT 'Table __EFMigrationsHistory created.';
END
GO

-- ============================================
-- Seed: Loại hàng hóa mẫu
-- ============================================
IF NOT EXISTS (SELECT 1 FROM Loai)
BEGIN
    INSERT INTO Loai (TenLoai, TenLoaiAlias, MoTa)
    VALUES
        (N'Điện thoại',    'dien-thoai',  N'Điện thoại di động các loại'),
        (N'Máy tính bảng', 'may-tinh-bang', N'Tablet và iPad'),
        (N'Laptop',        'laptop',       N'Máy tính xách tay'),
        (N'Phụ kiện',      'phu-kien',     N'Phụ kiện điện tử'),
        (N'Đồng hồ thông minh', 'dong-ho-thong-minh', N'Smartwatch');

    PRINT 'Seed data for Loai inserted.';
END
GO

-- ============================================
-- Seed: Hàng hóa mẫu
-- ============================================
IF NOT EXISTS (SELECT 1 FROM HangHoa)
BEGIN
    INSERT INTO HangHoa (TenHH, TenAlias, MaLoai, MoTaDonVi, DonGia, NgaySX, GiamGia, LuotMua, MoTa)
    VALUES
        (N'iPhone 15 Pro Max 256GB', 'iphone-15-pro-max-256gb', 1, N'Chiếc', 34990000, '2023-09-22', 5, 120, N'Điện thoại cao cấp nhất của Apple'),
        (N'Samsung Galaxy S24 Ultra', 'samsung-galaxy-s24-ultra', 1, N'Chiếc', 31990000, '2024-01-17', 10, 85, N'Flagship mới nhất của Samsung'),
        (N'iPad Pro M4 11 inch', 'ipad-pro-m4-11-inch', 2, N'Chiếc', 24990000, '2024-05-15', 0, 45, N'iPad Pro với chip M4 mạnh mẽ'),
        (N'MacBook Air M3', 'macbook-air-m3', 3, N'Chiếc', 28990000, '2024-03-08', 8, 67, N'Laptop siêu mỏng nhẹ với chip M3'),
        (N'AirPods Pro 2', 'airpods-pro-2', 4, N'Đôi', 6290000, '2022-09-23', 15, 230, N'Tai nghe không dây cao cấp'),
        (N'Apple Watch Series 9', 'apple-watch-series-9', 5, N'Chiếc', 10990000, '2023-09-22', 5, 98, N'Đồng hồ thông minh Apple mới nhất');

    PRINT 'Seed data for HangHoa inserted.';
END
GO

PRINT '=== ProductServiceDb migration completed successfully! ===';
GO
