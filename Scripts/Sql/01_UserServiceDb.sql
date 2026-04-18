-- ============================================
-- UserService Database Migration Script
-- Database: UserServiceDb
-- ============================================

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'UserServiceDb')
BEGIN
    CREATE DATABASE UserServiceDb;
END
GO

USE UserServiceDb;
GO

-- ============================================
-- Table: NguoiDung
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NguoiDung')
BEGIN
    CREATE TABLE NguoiDung
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap NVARCHAR(50)  NOT NULL,
        MatKhau     NVARCHAR(255) NOT NULL,
        VaiTro      INT           NOT NULL DEFAULT 0,
        HieuLuc     BIT           NOT NULL DEFAULT 1,
        NgayTao     DATETIME      NOT NULL DEFAULT GETDATE(),
        RandomKey   VARCHAR(50)   NULL,

        CONSTRAINT UQ_NguoiDung_TenDangNhap UNIQUE (TenDangNhap)
    );

    PRINT 'Table NguoiDung created.';
END
ELSE
    PRINT 'Table NguoiDung already exists.';
GO

-- ============================================
-- Table: KhachHang
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KhachHang')
BEGIN
    CREATE TABLE KhachHang
    (
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
        CONSTRAINT FK_KhachHang_NguoiDung
            FOREIGN KEY (UserId) REFERENCES NguoiDung(Id)
            ON DELETE CASCADE
    );

    PRINT 'Table KhachHang created.';
END
ELSE
    PRINT 'Table KhachHang already exists.';
GO

-- ============================================
-- Table: NhanVien
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NhanVien')
BEGIN
    CREATE TABLE NhanVien
    (
        MaNV      NVARCHAR(50)  NOT NULL PRIMARY KEY,
        UserId    INT           NOT NULL,
        HoTen     NVARCHAR(100) NOT NULL,
        Email     NVARCHAR(100) NOT NULL,
        DienThoai NVARCHAR(20)  NULL,

        CONSTRAINT UQ_NhanVien_UserId UNIQUE (UserId),
        CONSTRAINT FK_NhanVien_NguoiDung
            FOREIGN KEY (UserId) REFERENCES NguoiDung(Id)
            ON DELETE CASCADE
    );

    PRINT 'Table NhanVien created.';
END
ELSE
    PRINT 'Table NhanVien already exists.';
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
-- Seed: Admin account (password: Admin@123)
-- BCrypt hash of "Admin@123"
-- ============================================
IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE TenDangNhap = 'admin')
BEGIN
    DECLARE @AdminId INT;

    INSERT INTO NguoiDung (TenDangNhap, MatKhau, VaiTro, HieuLuc, NgayTao)
    VALUES (
        'admin',
        '$2a$11$rQpMJGTiOkT/yFZqyflHOuCxJ5oYcAJV4LfJJvuiVpPbZGzqkmD7a',
        1,
        1,
        GETDATE()
    );

    SET @AdminId = SCOPE_IDENTITY();

    INSERT INTO NhanVien (MaNV, UserId, HoTen, Email, DienThoai)
    VALUES ('NV00001', @AdminId, N'Quản trị viên', 'admin@hkshop.com', NULL);

    PRINT 'Admin account seeded: admin / Admin@123';
END
ELSE
    PRINT 'Admin account already exists.';
GO

PRINT '=== UserServiceDb migration completed successfully! ===';
GO
