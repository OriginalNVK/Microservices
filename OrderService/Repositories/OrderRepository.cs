using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    IQueryable<HoaDon> HoaDons { get; }
    Task<HoaDon?> FindHoaDonByIdAsync(int maHD);
    Task AddHoaDonAsync(HoaDon hoaDon);
    Task AddChiTietAsync(ChiTietHD chiTiet);
    void RemoveHoaDon(HoaDon hoaDon);
    Task<int> SaveChangesAsync();
}

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public IQueryable<HoaDon> HoaDons => _context.HoaDons.AsQueryable();

    public Task<HoaDon?> FindHoaDonByIdAsync(int maHD) => _context.HoaDons.FindAsync(maHD).AsTask();

    public Task AddHoaDonAsync(HoaDon hoaDon) => _context.HoaDons.AddAsync(hoaDon).AsTask();

    public Task AddChiTietAsync(ChiTietHD chiTiet) => _context.ChiTietHDs.AddAsync(chiTiet).AsTask();

    public void RemoveHoaDon(HoaDon hoaDon) => _context.HoaDons.Remove(hoaDon);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
