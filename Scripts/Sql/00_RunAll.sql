-- ============================================
-- HKShop Microservices - Run All Migrations
-- Chạy file này để tạo toàn bộ databases
-- Server: localhost (hoặc IP SQL Server của bạn)
-- User: sa / Password: HKShop@2024!
-- ============================================

PRINT '====================================================';
PRINT ' HKShop Microservices - Database Migration';
PRINT ' Started: ' + CONVERT(NVARCHAR, GETDATE(), 120);
PRINT '====================================================';
PRINT '';

-- Chạy lần lượt từng file:
-- :r 01_UserServiceDb.sql
-- :r 02_ProductServiceDb.sql
-- :r 03_CartServiceDb.sql
-- :r 04_OrderServiceDb.sql
-- :r 05_InvoiceServiceDb.sql

-- ============================================
-- Hoặc chạy inline toàn bộ bên dưới:
-- ============================================

------------ 1. UserServiceDb ------------
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'UserServiceDb')
    CREATE DATABASE UserServiceDb;
GO
USE UserServiceDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NguoiDung')
BEGIN
    CREATE TABLE NguoiDung (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap NVARCHAR(50)  NOT NULL,
        MatKhau     NVARCHAR(255) NOT NULL,
        VaiTro      INT           NOT NULL DEFAULT 0,
        HieuLuc     BIT           NOT NULL DEFAULT 1,
        NgayTao     DATETIME      NOT NULL DEFAULT GETDATE(),
        RandomKey   VARCHAR(50)   NULL,
        CONSTRAINT UQ_NguoiDung_TenDangNhap UNIQUE (TenDangNhap)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KhachHang')
BEGIN
    CREATE TABLE KhachHang (
        MaKH      NVARCHAR(20)  NOT NULL PRIMARY KEY,
        UserId    INT           NOT NULL,
        HoTen     NVARCHAR(100) NOT NULL,
        GioiTinh  BIT           NOT NULL,
        NgaySinh  DATE          NOT NULL,
        DiaChi    NVARCHAR(200) NULL,
        DienThoai NVARCHAR(20)  NULL,
        Email     NVARCHAR(100) NOT NULL,
        Hinh      NVARCHAR(255) NULL,
        CONSTRAINT UQ_KhachHang_UserId UNIQUE (UserId),
        CONSTRAINT FK_KhachHang_NguoiDung FOREIGN KEY (UserId) REFERENCES NguoiDung(Id) ON DELETE CASCADE
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NhanVien')
BEGIN
    CREATE TABLE NhanVien (
        MaNV      NVARCHAR(50)  NOT NULL PRIMARY KEY,
        UserId    INT           NOT NULL,
        HoTen     NVARCHAR(100) NOT NULL,
        Email     NVARCHAR(100) NOT NULL,
        DienThoai NVARCHAR(20)  NULL,
        CONSTRAINT UQ_NhanVien_UserId UNIQUE (UserId),
        CONSTRAINT FK_NhanVien_NguoiDung FOREIGN KEY (UserId) REFERENCES NguoiDung(Id) ON DELETE CASCADE
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory (MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY, ProductVersion NVARCHAR(32) NOT NULL);
    INSERT INTO __EFMigrationsHistory VALUES ('20240101000000_InitialCreate', '8.0.0');
END
GO
-- Seed admin (password: Admin@123)
IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE TenDangNhap = 'admin')
BEGIN
    DECLARE @AdminId INT;
    INSERT INTO NguoiDung (TenDangNhap, MatKhau, VaiTro, HieuLuc, NgayTao)
    VALUES ('admin', '$2a$11$rQpMJGTiOkT/yFZqyflHOuCxJ5oYcAJV4LfJJvuiVpPbZGzqkmD7a', 1, 1, GETDATE());
    SET @AdminId = SCOPE_IDENTITY();
    INSERT INTO NhanVien (MaNV, UserId, HoTen, Email) VALUES ('NV00001', @AdminId, N'Quản trị viên', 'admin@hkshop.com');
END
GO
PRINT '[1/5] UserServiceDb - OK';
GO

------------ 2. ProductServiceDb ------------
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'ProductServiceDb')
    CREATE DATABASE ProductServiceDb;
GO
USE ProductServiceDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Loai')
BEGIN
    CREATE TABLE Loai (
        MaLoai       INT IDENTITY(1,1) PRIMARY KEY,
        TenLoai      NVARCHAR(50)  NOT NULL,
        TenLoaiAlias NVARCHAR(50)  NULL,
        MoTa         NVARCHAR(MAX) NULL,
        Hinh         NVARCHAR(MAX) NULL
    );
    INSERT INTO Loai (TenLoai, TenLoaiAlias, MoTa) VALUES
        (N'Điện thoại',         'dien-thoai',          N'Điện thoại di động'),
        (N'Máy tính bảng',      'may-tinh-bang',        N'Tablet và iPad'),
        (N'Laptop',             'laptop',               N'Máy tính xách tay'),
        (N'Phụ kiện',           'phu-kien',             N'Phụ kiện điện tử'),
        (N'Đồng hồ thông minh', 'dong-ho-thong-minh',   N'Smartwatch');
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HangHoa')
BEGIN
    CREATE TABLE HangHoa (
        MaHH      INT IDENTITY(1,1) PRIMARY KEY,
        TenHH     NVARCHAR(100)  NOT NULL,
        TenAlias  NVARCHAR(100)  NULL,
        MaLoai    INT            NOT NULL,
        MoTaDonVi NVARCHAR(50)   NULL,
        DonGia    DECIMAL(18,2)  NULL,
        Hinh      NVARCHAR(255)  NULL,
        NgaySX    DATE           NOT NULL,
        GiamGia   DECIMAL(5,2)   NOT NULL DEFAULT 0,
        LuotMua   INT            NOT NULL DEFAULT 0,
        MoTa      NVARCHAR(MAX)  NULL,
        CONSTRAINT FK_HangHoa_Loai FOREIGN KEY (MaLoai) REFERENCES Loai(MaLoai)
    );
    INSERT INTO HangHoa (TenHH, TenAlias, MaLoai, MoTaDonVi, DonGia, NgaySX, GiamGia, LuotMua, MoTa) VALUES
        (N'iPhone 15 Pro Max 256GB', 'iphone-15-pro-max', 1, N'Chiếc', 34990000, '2023-09-22', 5,  120, N'Điện thoại cao cấp Apple'),
        (N'Samsung Galaxy S24 Ultra','samsung-s24-ultra',  1, N'Chiếc', 31990000, '2024-01-17', 10, 85,  N'Flagship Samsung mới nhất'),
        (N'iPad Pro M4 11 inch',     'ipad-pro-m4',        2, N'Chiếc', 24990000, '2024-05-15', 0,  45,  N'iPad với chip M4'),
        (N'MacBook Air M3',          'macbook-air-m3',     3, N'Chiếc', 28990000, '2024-03-08', 8,  67,  N'Laptop siêu mỏng M3'),
        (N'AirPods Pro 2',           'airpods-pro-2',      4, N'Đôi',   6290000,  '2022-09-23', 15, 230, N'Tai nghe không dây'),
        (N'Apple Watch Series 9',    'apple-watch-s9',     5, N'Chiếc', 10990000, '2023-09-22', 5,  98,  N'Smartwatch Apple');
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory (MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY, ProductVersion NVARCHAR(32) NOT NULL);
    INSERT INTO __EFMigrationsHistory VALUES ('20240101000000_InitialCreate', '8.0.0');
END
GO
PRINT '[2/5] ProductServiceDb - OK';
GO

------------ 3. CartServiceDb ------------
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'CartServiceDb')
    CREATE DATABASE CartServiceDb;
GO
USE CartServiceDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HangHoaCache')
BEGIN
    CREATE TABLE HangHoaCache (
        MaHH      INT           NOT NULL PRIMARY KEY,
        TenHH     NVARCHAR(100) NOT NULL,
        DonGia    DECIMAL(18,2) NOT NULL DEFAULT 0,
        GiamGia   DECIMAL(5,2)  NOT NULL DEFAULT 0,
        Hinh      NVARCHAR(255) NULL,
        UpdatedAt DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Cart')
BEGIN
    CREATE TABLE Cart (
        MaCart   INT IDENTITY(1,1) PRIMARY KEY,
        MaKH     NVARCHAR(20)  NOT NULL,
        MaHH     INT           NOT NULL,
        SoLuong  INT           NOT NULL CONSTRAINT CHK_Cart_SoLuong CHECK (SoLuong > 0),
        DonGia   DECIMAL(18,2) NOT NULL,
        NgayThem DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_Cart_KhachHang_HangHoa UNIQUE (MaKH, MaHH),
        CONSTRAINT FK_Cart_HangHoaCache FOREIGN KEY (MaHH) REFERENCES HangHoaCache(MaHH)
    );
    CREATE INDEX IX_Cart_MaKH ON Cart(MaKH);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory (MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY, ProductVersion NVARCHAR(32) NOT NULL);
    INSERT INTO __EFMigrationsHistory VALUES ('20240101000000_InitialCreate', '8.0.0');
END
GO
PRINT '[3/5] CartServiceDb - OK';
GO

------------ 4. OrderServiceDb ------------
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'OrderServiceDb')
    CREATE DATABASE OrderServiceDb;
GO
USE OrderServiceDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HoaDon')
BEGIN
    CREATE TABLE HoaDon (
        MaHD          INT IDENTITY(1,1) PRIMARY KEY,
        MaKH          NVARCHAR(20)  NOT NULL,
        NgayDat       DATETIME      NOT NULL DEFAULT GETDATE(),
        NgayCan       DATE          NULL,
        NgayGiao      DATE          NULL,
        HoTen         NVARCHAR(100) NULL,
        DiaChi        NVARCHAR(200) NOT NULL,
        CachThanhToan NVARCHAR(50)  NOT NULL,
        CachVanChuyen NVARCHAR(50)  NOT NULL,
        PhiVanChuyen  DECIMAL(18,2) NOT NULL DEFAULT 0,
        MaNV          NVARCHAR(50)  NULL,
        GhiChu        NVARCHAR(500) NULL,
        TrangThai     INT           NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_HoaDon_MaKH      ON HoaDon(MaKH);
    CREATE INDEX IX_HoaDon_NgayDat   ON HoaDon(NgayDat);
    CREATE INDEX IX_HoaDon_TrangThai ON HoaDon(TrangThai);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ChiTietHD')
BEGIN
    CREATE TABLE ChiTietHD (
        MaCT    INT IDENTITY(1,1) PRIMARY KEY,
        MaHD    INT           NOT NULL,
        MaHH    INT           NOT NULL,
        TenHH   NVARCHAR(100) NOT NULL,
        DonGia  DECIMAL(18,2) NOT NULL,
        SoLuong INT           NOT NULL CONSTRAINT CHK_ChiTietHD_SoLuong_Order CHECK (SoLuong > 0),
        GiamGia DECIMAL(5,2)  NOT NULL DEFAULT 0,
        CONSTRAINT FK_ChiTietHD_HoaDon_Order FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD) ON DELETE CASCADE
    );
    CREATE INDEX IX_ChiTietHD_MaHD ON ChiTietHD(MaHD);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory (MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY, ProductVersion NVARCHAR(32) NOT NULL);
    INSERT INTO __EFMigrationsHistory VALUES ('20240101000000_InitialCreate', '8.0.0');
END
GO
PRINT '[4/5] OrderServiceDb - OK';
GO

------------ 5. InvoiceServiceDb ------------
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'InvoiceServiceDb')
    CREATE DATABASE InvoiceServiceDb;
GO
USE InvoiceServiceDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HoaDon')
BEGIN
    CREATE TABLE HoaDon (
        MaHD          INT           NOT NULL PRIMARY KEY,
        MaKH          NVARCHAR(20)  NOT NULL,
        HoTenKH       NVARCHAR(100) NULL,
        EmailKH       NVARCHAR(100) NULL,
        DienThoaiKH   NVARCHAR(20)  NULL,
        NgayDat       DATETIME      NOT NULL,
        NgayCan       DATE          NULL,
        NgayGiao      DATE          NULL,
        HoTen         NVARCHAR(100) NULL,
        DiaChi        NVARCHAR(200) NOT NULL,
        CachThanhToan NVARCHAR(50)  NOT NULL,
        CachVanChuyen NVARCHAR(50)  NOT NULL,
        PhiVanChuyen  DECIMAL(18,2) NOT NULL DEFAULT 0,
        MaNV          NVARCHAR(50)  NULL,
        GhiChu        NVARCHAR(500) NULL,
        TrangThai     INT           NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_HoaDon_MaKH      ON HoaDon(MaKH);
    CREATE INDEX IX_HoaDon_NgayDat   ON HoaDon(NgayDat);
    CREATE INDEX IX_HoaDon_TrangThai ON HoaDon(TrangThai);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ChiTietHD')
BEGIN
    CREATE TABLE ChiTietHD (
        MaCT    INT IDENTITY(1,1) PRIMARY KEY,
        MaHD    INT           NOT NULL,
        MaHH    INT           NOT NULL,
        TenHH   NVARCHAR(100) NOT NULL,
        DonGia  DECIMAL(18,2) NOT NULL,
        SoLuong INT           NOT NULL CONSTRAINT CHK_ChiTietHD_SoLuong_Invoice CHECK (SoLuong > 0),
        GiamGia DECIMAL(5,2)  NOT NULL DEFAULT 0,
        CONSTRAINT FK_ChiTietHD_HoaDon_Invoice FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD) ON DELETE CASCADE
    );
    CREATE INDEX IX_ChiTietHD_MaHD ON ChiTietHD(MaHD);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory (MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY, ProductVersion NVARCHAR(32) NOT NULL);
    INSERT INTO __EFMigrationsHistory VALUES ('20240101000000_InitialCreate', '8.0.0');
END
GO
PRINT '[5/5] InvoiceServiceDb - OK';
GO

PRINT '';
PRINT '====================================================';
PRINT ' All databases migrated successfully!';
PRINT ' Completed: ' + CONVERT(NVARCHAR, GETDATE(), 120);
PRINT '====================================================';
GO
