-- ============================================
-- InvoiceService Database Migration Script
-- Database: InvoiceServiceDb
-- ============================================

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'InvoiceServiceDb')
BEGIN
    CREATE DATABASE InvoiceServiceDb;
END
GO

USE InvoiceServiceDb;
GO

-- ============================================
-- Table: HoaDon
-- (Dữ liệu được đồng bộ từ OrderService qua Kafka)
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HoaDon')
BEGIN
    CREATE TABLE HoaDon
    (
        MaHD           INT           NOT NULL PRIMARY KEY,
        MaKH           NVARCHAR(20)  NOT NULL,
        HoTenKH        NVARCHAR(100) NULL,
        EmailKH        NVARCHAR(100) NULL,
        DienThoaiKH    NVARCHAR(20)  NULL,
        NgayDat        DATETIME      NOT NULL,
        NgayCan        DATE          NULL,
        NgayGiao       DATE          NULL,
        HoTen          NVARCHAR(100) NULL,
        DiaChi         NVARCHAR(200) NOT NULL,
        CachThanhToan  NVARCHAR(50)  NOT NULL,
        CachVanChuyen  NVARCHAR(50)  NOT NULL,
        PhiVanChuyen   DECIMAL(18,2) NOT NULL DEFAULT 0,
        MaNV           NVARCHAR(50)  NULL,
        GhiChu         NVARCHAR(500) NULL,
        -- TrangThai: 0=Chờ xác nhận, 1=Đã xác nhận,
        --            2=Đang giao, 3=Đã giao, 4=Đã hủy
        TrangThai      INT           NOT NULL DEFAULT 0
    );

    CREATE INDEX IX_HoaDon_MaKH      ON HoaDon(MaKH);
    CREATE INDEX IX_HoaDon_NgayDat   ON HoaDon(NgayDat);
    CREATE INDEX IX_HoaDon_TrangThai ON HoaDon(TrangThai);

    PRINT 'Table HoaDon created.';
END
ELSE
    PRINT 'Table HoaDon already exists.';
GO

-- ============================================
-- Table: ChiTietHD
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ChiTietHD')
BEGIN
    CREATE TABLE ChiTietHD
    (
        MaCT    INT IDENTITY(1,1) PRIMARY KEY,
        MaHD    INT           NOT NULL,
        MaHH    INT           NOT NULL,
        TenHH   NVARCHAR(100) NOT NULL,
        DonGia  DECIMAL(18,2) NOT NULL,
        SoLuong INT           NOT NULL,
        GiamGia DECIMAL(5,2)  NOT NULL DEFAULT 0,

        CONSTRAINT CHK_ChiTietHD_SoLuong CHECK (SoLuong > 0),
        CONSTRAINT FK_ChiTietHD_HoaDon
            FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_ChiTietHD_MaHD ON ChiTietHD(MaHD);
    CREATE INDEX IX_ChiTietHD_MaHH ON ChiTietHD(MaHH);

    PRINT 'Table ChiTietHD created.';
END
ELSE
    PRINT 'Table ChiTietHD already exists.';
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

PRINT '=== InvoiceServiceDb migration completed successfully! ===';
GO
