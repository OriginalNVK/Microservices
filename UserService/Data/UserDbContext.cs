using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDungs { get; set; }
    public DbSet<KhachHang> KhachHangs { get; set; }
    public DbSet<NhanVien> NhanViens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.ToTable("NguoiDung");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(e => e.TenDangNhap).IsUnique();
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.ToTable("KhachHang");
            entity.HasKey(e => e.MaKH);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.NguoiDung)
                  .WithOne(n => n.KhachHang)
                  .HasForeignKey<KhachHang>(k => k.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.ToTable("NhanVien");
            entity.HasKey(e => e.MaNV);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.NguoiDung)
                  .WithOne(n => n.NhanVien)
                  .HasForeignKey<NhanVien>(nv => nv.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
