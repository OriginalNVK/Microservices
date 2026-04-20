using Microsoft.EntityFrameworkCore;
using InvoiceService.Models;

namespace InvoiceService.Data;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options) { }

    public DbSet<HoaDon> HoaDons { get; set; }
    public DbSet<ChiTietHD> ChiTietHDs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.ToTable("HoaDon");
            entity.HasKey(e => e.MaHD);
            entity.Property(e => e.MaHD).UseIdentityByDefaultColumn();
        });

        modelBuilder.Entity<ChiTietHD>(entity =>
        {
            entity.ToTable("ChiTietHD");
            entity.HasKey(e => e.MaCT);
            entity.Property(e => e.MaCT).UseIdentityByDefaultColumn();
            entity.HasOne(e => e.HoaDon)
                  .WithMany(h => h.ChiTietHDs)
                  .HasForeignKey(e => e.MaHD)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
