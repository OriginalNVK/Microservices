using ProductService.Data;
using ProductService.Models;

namespace ProductService.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public IQueryable<Category> Categories => _context.Categories.AsQueryable();
    public IQueryable<Product> Products => _context.Products.AsQueryable();

    public Task<Category?> FindCategoryByIdAsync(int categoryId) => _context.Categories.FindAsync(categoryId).AsTask();
    public Task<Product?> FindProductByIdAsync(int productId) => _context.Products.FindAsync(productId).AsTask();

    public Task AddCategoryAsync(Category category) => _context.Categories.AddAsync(category).AsTask();
    public Task AddProductAsync(Product product) => _context.Products.AddAsync(product).AsTask();

    public void RemoveCategory(Category category) => _context.Categories.Remove(category);
    public void RemoveProduct(Product product) => _context.Products.Remove(product);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
