using InvoiceService.Data;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Repositories;

public interface IInvoiceRepository
{
    IQueryable<HoaDon> HoaDons { get; }
    IQueryable<ChiTietHD> ChiTietHDs { get; }
    Task<int> SaveChangesAsync();
}

public class InvoiceRepository : IInvoiceRepository
{
    private readonly InvoiceDbContext _context;

    public InvoiceRepository(InvoiceDbContext context)
    {
        _context = context;
    }

    public IQueryable<HoaDon> HoaDons => _context.HoaDons.AsQueryable();
    public IQueryable<ChiTietHD> ChiTietHDs => _context.ChiTietHDs.AsQueryable();

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
