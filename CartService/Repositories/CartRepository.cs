using CartService.Data;
using CartService.Models;
using Microsoft.EntityFrameworkCore;

namespace CartService.Repositories;

public interface ICartRepository
{
    IQueryable<Cart> Carts { get; }
    IQueryable<HangHoaCache> HangHoaCaches { get; }
    Task<HangHoaCache?> FindHangHoaCacheAsync(int maHH);
    Task<Cart?> FindCartAsync(int maCart, string maKH);
    Task<Cart?> FindCartByCustomerAndProductAsync(string maKH, int maHH);
    Task AddCartAsync(Cart cart);
    void RemoveCart(Cart cart);
    void RemoveCarts(IEnumerable<Cart> carts);
    Task<int> SaveChangesAsync();
}

public class CartRepository : ICartRepository
{
    private readonly CartDbContext _context;

    public CartRepository(CartDbContext context)
    {
        _context = context;
    }

    public IQueryable<Cart> Carts => _context.Carts.AsQueryable();
    public IQueryable<HangHoaCache> HangHoaCaches => _context.HangHoaCaches.AsQueryable();

    public Task<HangHoaCache?> FindHangHoaCacheAsync(int maHH) => _context.HangHoaCaches.FindAsync(maHH).AsTask();

    public Task<Cart?> FindCartAsync(int maCart, string maKH) =>
        _context.Carts.FirstOrDefaultAsync(c => c.MaCart == maCart && c.MaKH == maKH);

    public Task<Cart?> FindCartByCustomerAndProductAsync(string maKH, int maHH) =>
        _context.Carts.FirstOrDefaultAsync(c => c.MaKH == maKH && c.MaHH == maHH);

    public Task AddCartAsync(Cart cart) => _context.Carts.AddAsync(cart).AsTask();

    public void RemoveCart(Cart cart) => _context.Carts.Remove(cart);

    public void RemoveCarts(IEnumerable<Cart> carts) => _context.Carts.RemoveRange(carts);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
