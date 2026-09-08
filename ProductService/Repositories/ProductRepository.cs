using Microsoft.EntityFrameworkCore;
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

    public Product? GetById(int id)
    {
        return _context.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
    }

    public Product Add(Product product)
    {
        _context.Products.Add(product);
        _context.SaveChanges();
        return product;
    }

    public Product Update(Product product)
    {
        _context.Products.Update(product);
        _context.SaveChanges();
        return product;
    }
    public bool Delete(Product product)
    {
        _context.Products.Remove(product);
        _context.SaveChanges();
        return true;
    }

    public List<Product> GetAll()
    {
        return _context.Products.Include(p => p.Category).ToList();
    }

    public IQueryable<Product> Query()
    {
        return _context.Products.AsQueryable();
    }
}
