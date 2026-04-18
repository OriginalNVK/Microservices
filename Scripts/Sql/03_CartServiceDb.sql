-- ============================================
-- CartService Database Migration Script
-- Database: CartServiceDb
-- ============================================

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'CartServiceDb')
BEGIN
    CREATE DATABASE CartServiceDb;
END
GO

USE CartServiceDb;
GO

-- ============================================
-- Table: HangHoaCache
-- (Dữ liệu sản phẩm được đồng bộ qua Kafka
--  từ ProductService)
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HangHoaCache')
BEGIN
    CREATE TABLE HangHoaCache
    (
        MaHH      INT           NOT NULL PRIMARY KEY,
        TenHH     NVARCHAR(100) NOT NULL,
        DonGia    DECIMAL(18,2) NOT NULL DEFAULT 0,
        GiamGia   DECIMAL(5,2)  NOT NULL DEFAULT 0,
        Hinh      NVARCHAR(255) NULL,
        UpdatedAt DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
    );

    PRINT 'Table HangHoaCache created.';
END
ELSE
    PRINT 'Table HangHoaCache already exists.';
GO

-- ============================================
-- Table: Cart
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Cart')
BEGIN
    CREATE TABLE Cart
    (
        MaCart   INT IDENTITY(1,1) PRIMARY KEY,
        MaKH     NVARCHAR(20)  NOT NULL,
        MaHH     INT           NOT NULL,
        SoLuong  INT           NOT NULL,
        DonGia   DECIMAL(18,2) NOT NULL,
        NgayThem DATETIME      NOT NULL DEFAULT GETDATE(),

        CONSTRAINT CHK_Cart_SoLuong CHECK (SoLuong > 0),
        CONSTRAINT UQ_Cart_KhachHang_HangHoa UNIQUE (MaKH, MaHH),
        CONSTRAINT FK_Cart_HangHoaCache
            FOREIGN KEY (MaHH) REFERENCES HangHoaCache(MaHH)
    );

    CREATE INDEX IX_Cart_MaKH ON Cart(MaKH);

    PRINT 'Table Cart created.';
END
ELSE
    PRINT 'Table Cart already exists.';
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

PRINT '=== CartServiceDb migration completed successfully! ===';
GO
