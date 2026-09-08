using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ProductDbContext _context;

    public CategoryRepository(ProductDbContext context)
    {
        _context = context;
    }

    public Category? GetById(int id)
    {
        return _context.Categories.FirstOrDefault(c => c.Id == id);
    } 

    public Category Add(Category category)
    {
        _context.Categories.Add(category);
        _context.SaveChanges();
        return category;
    }

    public Category Update(Category category)
    {
        _context.Categories.Update(category);
        _context.SaveChanges();
        return category;
    }

    public bool Delete(Category category)
    {
        _context.Categories.Remove(category);
        _context.SaveChanges();
        return true;
    }

    public List<Category> GetAll()
    {
        return _context.Categories.AsNoTracking().ToList();  
    } 

    public IQueryable<Category> Query()
    {
        return _context.Categories.AsQueryable();
    }
}