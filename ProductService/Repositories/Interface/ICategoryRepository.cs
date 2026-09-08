using ProductService.Models;

namespace ProductService.Repositories;

public interface ICategoryRepository
{
    IQueryable<Category> Query();
    List<Category> GetAll();
    Category? GetById(int id);
    Category Add(Category category);
    Category Update(Category category);
    bool Delete(Category category);
}
