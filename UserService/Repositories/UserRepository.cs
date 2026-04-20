using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using UserService.Data;
using UserService.Models;

namespace UserService.Repositories;

public interface IUserRepository
{
    IQueryable<NguoiDung> NguoiDungs { get; }
    IQueryable<KhachHang> KhachHangs { get; }
    IQueryable<NhanVien> NhanViens { get; }
    Task<NguoiDung?> FindUserByIdAsync(int userId);
    Task AddUserAsync(NguoiDung user);
    Task AddCustomerAsync(KhachHang customer);
    Task AddEmployeeAsync(NhanVien employee);
    void RemoveUser(NguoiDung user);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<int> SaveChangesAsync();
}

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    public IQueryable<NguoiDung> NguoiDungs => _context.NguoiDungs.AsQueryable();
    public IQueryable<KhachHang> KhachHangs => _context.KhachHangs.AsQueryable();
    public IQueryable<NhanVien> NhanViens => _context.NhanViens.AsQueryable();

    public Task<NguoiDung?> FindUserByIdAsync(int userId) => _context.NguoiDungs.FindAsync(userId).AsTask();

    public Task AddUserAsync(NguoiDung user) => _context.NguoiDungs.AddAsync(user).AsTask();
    public Task AddCustomerAsync(KhachHang customer) => _context.KhachHangs.AddAsync(customer).AsTask();
    public Task AddEmployeeAsync(NhanVien employee) => _context.NhanViens.AddAsync(employee).AsTask();

    public void RemoveUser(NguoiDung user) => _context.NguoiDungs.Remove(user);

    public Task<IDbContextTransaction> BeginTransactionAsync() => _context.Database.BeginTransactionAsync();

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
