using ProductService.Models;

namespace ProductService.Repositories;

public interface IProductRepository
{
	IQueryable<Product> Query();
	List<Product> GetAll();
	Product? GetById(int id);
	Product Add(Product product);
	Product Update(Product product);
	bool Delete(Product product);
}