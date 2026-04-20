using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Repositories;

public interface IProductRepository
{
    IQueryable<Loai> Loais { get; }
    IQueryable<HangHoa> HangHoas { get; }
    Task<Loai?> FindLoaiByIdAsync(int maLoai);
    Task<HangHoa?> FindHangHoaByIdAsync(int maHH);
    Task AddLoaiAsync(Loai loai);
    Task AddHangHoaAsync(HangHoa hangHoa);
    void RemoveLoai(Loai loai);
    void RemoveHangHoa(HangHoa hangHoa);
    Task<int> SaveChangesAsync();
}

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public IQueryable<Loai> Loais => _context.Loais.AsQueryable();
    public IQueryable<HangHoa> HangHoas => _context.HangHoas.AsQueryable();

    public Task<Loai?> FindLoaiByIdAsync(int maLoai) => _context.Loais.FindAsync(maLoai).AsTask();
    public Task<HangHoa?> FindHangHoaByIdAsync(int maHH) => _context.HangHoas.FindAsync(maHH).AsTask();

    public Task AddLoaiAsync(Loai loai) => _context.Loais.AddAsync(loai).AsTask();
    public Task AddHangHoaAsync(HangHoa hangHoa) => _context.HangHoas.AddAsync(hangHoa).AsTask();

    public void RemoveLoai(Loai loai) => _context.Loais.Remove(loai);
    public void RemoveHangHoa(HangHoa hangHoa) => _context.HangHoas.Remove(hangHoa);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
