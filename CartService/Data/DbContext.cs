using Microsoft.EntityFrameworkCore;
using CartService.Models;

namespace CartService.Data;

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options) { }

    public DbSet<Cart> Carts { get; set; }
    public DbSet<HangHoaCache> HangHoaCaches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Cart");
            entity.HasKey(e => e.MaCart);
            entity.Property(e => e.MaCart).UseIdentityByDefaultColumn();
            entity.HasIndex(e => new { e.MaKH, e.MaHH }).IsUnique();
        });

        modelBuilder.Entity<HangHoaCache>(entity =>
        {
            entity.ToTable("HangHoaCache");
            entity.HasKey(e => e.MaHH);
        });
    }
}
