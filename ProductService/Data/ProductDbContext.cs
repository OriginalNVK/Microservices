using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Loai> Loais { get; set; }
    public DbSet<HangHoa> HangHoas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Loai>(entity =>
        {
            entity.ToTable("Loai");
            entity.HasKey(e => e.MaLoai);
            entity.Property(e => e.MaLoai).UseIdentityColumn(1, 1);
        });

        modelBuilder.Entity<HangHoa>(entity =>
        {
            entity.ToTable("HangHoa");
            entity.HasKey(e => e.MaHH);
            entity.Property(e => e.MaHH).UseIdentityColumn(1, 1);
            entity.HasOne(e => e.Loai)
                  .WithMany(l => l.HangHoas)
                  .HasForeignKey(e => e.MaLoai)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
