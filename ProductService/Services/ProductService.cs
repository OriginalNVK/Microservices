using Microsoft.EntityFrameworkCore;
using ProductService.DTOs;
using ProductService.Kafka;
using ProductService.Models;
using ProductService.Repositories;

namespace ProductService.Services;

public class ProductQueryFilter
{
	public int? CategoryId { get; set; }
	public string? Search { get; set; }
	public decimal? MinPrice { get; set; }
	public decimal? MaxPrice { get; set; }
	public string? SortBy { get; set; } = "name";
	public bool Ascending { get; set; } = true;
	public int Page { get; set; } = 1;
	public int Size { get; set; } = 12;
}

public class ProductService : IProductService
{
	private readonly IProductRepository _repository;
	private readonly IKafkaProducer _kafkaProducer;

	public ProductService(IProductRepository repository, IKafkaProducer kafkaProducer)
	{
		_repository = repository;
		_kafkaProducer = kafkaProducer;
	}

	public async Task<object> GetAllProductsAsync(ProductQueryFilter filter)
	{
		var query = _repository.Products.Include(p => p.Category).AsQueryable();

		if (filter.CategoryId.HasValue)
			query = query.Where(p => p.CategoryId == filter.CategoryId);
		if (!string.IsNullOrEmpty(filter.Search))
			query = query.Where(p => p.ProductName.Contains(filter.Search) || (p.Description != null && p.Description.Contains(filter.Search)));
		if (filter.MinPrice.HasValue)
			query = query.Where(p => p.Price >= filter.MinPrice);
		if (filter.MaxPrice.HasValue)
			query = query.Where(p => p.Price <= filter.MaxPrice);

		query = filter.SortBy?.ToLower() switch
		{
			"price" => filter.Ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
			"purchasecount" => filter.Ascending ? query.OrderBy(p => p.PurchaseCount) : query.OrderByDescending(p => p.PurchaseCount),
			"discount" => filter.Ascending ? query.OrderBy(p => p.Discount) : query.OrderByDescending(p => p.Discount),
			"createddate" => filter.Ascending ? query.OrderBy(p => p.CreatedDate) : query.OrderByDescending(p => p.CreatedDate),
			_ => filter.Ascending ? query.OrderBy(p => p.ProductName) : query.OrderByDescending(p => p.ProductName)
		};

		var total = await query.CountAsync();
		var entities = await query
			.Skip((filter.Page - 1) * filter.Size)
			.Take(filter.Size)
			.ToListAsync();

		var items = entities.Select(MapProductResponse).ToList();

		return new { total, page = filter.Page, size = filter.Size, items };
	}

	public Task<ProductResponseDto?> GetProductByIdAsync(int productId) =>
		_repository.Products
			.Include(p => p.Category)
			.Where(p => p.ProductId == productId)
			.Select(p => new ProductResponseDto
			{
				ProductId = p.ProductId,
				ProductName = p.ProductName,
				ProductAlias = p.ProductAlias,
				CategoryId = p.CategoryId,
				CategoryName = p.Category.CategoryName,
				DescriptionUnit = p.DescriptionUnit,
				Price = p.Price,
				Image = p.Image,
				CreatedDate = p.CreatedDate,
				Discount = p.Discount,
				PurchaseCount = p.PurchaseCount,
				Description = p.Description
			})
			.FirstOrDefaultAsync();

	public async Task<(bool Success, string Message, int ProductId)> CreateProductAsync(CreateProductDto dto)
	{
		if (!await _repository.Categories.AnyAsync(l => l.CategoryId == dto.CategoryId))
			return (false, "Category does not exist", 0);

		var product = new Product
		{
			ProductName = dto.ProductName,
			ProductAlias = dto.ProductAlias ?? dto.ProductName.ToLower().Replace(" ", "-"),
			CategoryId = dto.CategoryId,
			DescriptionUnit = dto.DescriptionUnit,
			Price = dto.Price,
			Image = dto.Image,
			CreatedDate = dto.CreatedDate,
			Discount = dto.Discount,
			PurchaseCount = 0,
			Description = dto.Description
		};

		await _repository.AddProductAsync(product);
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("product.created", new
		{
			ProductId = product.ProductId,
			ProductName = product.ProductName,
			Price = product.Price,
			Discount = product.Discount,
			Image = product.Image,
			CreatedAt = DateTime.UtcNow
		});

		return (true, "Product created successfully", product.ProductId);
	}

	public async Task<(bool Success, string Message)> UpdateProductAsync(int productId, UpdateProductDto dto)
	{
		var product = await _repository.FindProductByIdAsync(productId);
		if (product == null) return (false, "Product not found");

		if (dto.CategoryId.HasValue && !await _repository.Categories.AnyAsync(l => l.CategoryId == dto.CategoryId))
			return (false, "Category does not exist");

		if (dto.ProductName != null) product.ProductName = dto.ProductName;
		if (dto.ProductAlias != null) product.ProductAlias = dto.ProductAlias;
		if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;
		if (dto.DescriptionUnit != null) product.DescriptionUnit = dto.DescriptionUnit;
		if (dto.Price.HasValue) product.Price = dto.Price;
		if (dto.Image != null) product.Image = dto.Image;
		if (dto.CreatedDate.HasValue) product.CreatedDate = dto.CreatedDate.Value;
		if (dto.Discount.HasValue) product.Discount = dto.Discount.Value;
		if (dto.Description != null) product.Description = dto.Description;

		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("product.updated", new
		{
			ProductId = product.ProductId,
			ProductName = product.ProductName,
			Price = product.Price,
			Discount = product.Discount,
			Image = product.Image,
			UpdatedAt = DateTime.UtcNow
		});

		return (true, "Product updated successfully");
	}

	public async Task<bool> DeleteProductAsync(int productId)
	{
		var product = await _repository.FindProductByIdAsync(productId);
		if (product == null) return false;

		_repository.RemoveProduct(product);
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("product.deleted", new
		{
			ProductId = productId,
			DeletedAt = DateTime.UtcNow
		});

		return true;
	}

	public Task<List<ProductResponseDto>> GetBestSellersAsync(int top) =>
		_repository.Products
			.Include(p => p.Category)
			.OrderByDescending(p => p.PurchaseCount)
			.Take(top)
			.Select(p => new ProductResponseDto
			{
				ProductId = p.ProductId,
				ProductName = p.ProductName,
				ProductAlias = p.ProductAlias,
				CategoryId = p.CategoryId,
				CategoryName = p.Category.CategoryName,
				DescriptionUnit = p.DescriptionUnit,
				Price = p.Price,
				Image = p.Image,
				CreatedDate = p.CreatedDate,
				Discount = p.Discount,
				PurchaseCount = p.PurchaseCount,
				Description = p.Description
			})
			.ToListAsync();

	public Task<List<ProductResponseDto>> GetOnSaleAsync() =>
		_repository.Products
			.Include(p => p.Category)
			.Where(p => p.Discount > 0)
			.OrderByDescending(p => p.Discount)
			.Select(p => new ProductResponseDto
			{
				ProductId = p.ProductId,
				ProductName = p.ProductName,
				ProductAlias = p.ProductAlias,
				CategoryId = p.CategoryId,
				CategoryName = p.Category.CategoryName,
				DescriptionUnit = p.DescriptionUnit,
				Price = p.Price,
				Image = p.Image,
				CreatedDate = p.CreatedDate,
				Discount = p.Discount,
				PurchaseCount = p.PurchaseCount,
				Description = p.Description
			})
			.ToListAsync();

	public async Task<List<object>> GetAllCategoriesAsync()
	{
		var categories = await _repository.Categories
			.Select(c => new
			{
				c.CategoryId,
				c.CategoryName,
				c.CategoryAlias,
				c.Description,
				c.Image,
				ProductCount = c.Products.Count
			})
			.ToListAsync();

		return categories.Cast<object>().ToList();
	}

	public async Task<object?> GetCategoryByIdAsync(int categoryId)
	{
		var category = await _repository.Categories
			.Include(c => c.Products)
			.FirstOrDefaultAsync(c => c.CategoryId == categoryId);

		if (category == null) return null;

		return new
		{
			category.CategoryId,
			category.CategoryName,
			category.CategoryAlias,
			category.Description,
			category.Image,
			ProductCount = category.Products.Count,
			Products = category.Products.Select(p => new
			{
				p.ProductId,
				p.ProductName,
				p.ProductAlias,
				p.Price,
				p.Discount,
				p.Image,
				p.PurchaseCount
			})
		};
	}

	public async Task<Category> CreateCategoryAsync(CreateCategoryDto dto)
	{
		var category = new Category
		{
			CategoryName = dto.CategoryName,
			CategoryAlias = dto.CategoryAlias ?? dto.CategoryName.ToLower().Replace(" ", "-"),
			Description = dto.Description,
			Image = dto.Image
		};

		await _repository.AddCategoryAsync(category);
		await _repository.SaveChangesAsync();
		return category;
	}

	public async Task<Category?> UpdateCategoryAsync(int categoryId, UpdateCategoryDto dto)
	{
		var category = await _repository.FindCategoryByIdAsync(categoryId);
		if (category == null) return null;

		if (dto.CategoryName != null) category.CategoryName = dto.CategoryName;
		if (dto.CategoryAlias != null) category.CategoryAlias = dto.CategoryAlias;
		if (dto.Description != null) category.Description = dto.Description;
		if (dto.Image != null) category.Image = dto.Image;

		await _repository.SaveChangesAsync();
		return category;
	}

	public async Task<(bool Success, string Message)> DeleteCategoryAsync(int categoryId)
	{
		var category = await _repository.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
		if (category == null) return (false, "Category not found");

		if (category.Products.Any())
			return (false, "Cannot delete a category that has products");

		_repository.RemoveCategory(category);
		await _repository.SaveChangesAsync();

		return (true, "Category deleted successfully");
	}

	private static ProductResponseDto MapProductResponse(Product product) => new()
	{
		ProductId = product.ProductId,
		ProductName = product.ProductName,
		ProductAlias = product.ProductAlias,
		CategoryId = product.CategoryId,
		CategoryName = product.Category.CategoryName,
		DescriptionUnit = product.DescriptionUnit,
		Price = product.Price,
		Image = product.Image,
		CreatedDate = product.CreatedDate,
		Discount = product.Discount,
		PurchaseCount = product.PurchaseCount,
		Description = product.Description
	};
}
