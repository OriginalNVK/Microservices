using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories;

namespace ProductService.Services;

public class CategoryService : ICategoryService
{
	private readonly ICategoryRepository _categoryRepository;

	public CategoryService(ICategoryRepository categoryRepository)
	{
		_categoryRepository = categoryRepository;
	}

	public List<Category> GetAllCategories()
	{
		return _categoryRepository.GetAll();
	}

	public Category? GetCategoryById(int categoryId)
	{
		return _categoryRepository.GetById(categoryId);
	}

	public Category CreateCategory(string categoryName)
	{
		var existingCategory = _categoryRepository.Query().FirstOrDefault(c => c.Name == categoryName);
		if (existingCategory != null)
		{
			throw new InvalidOperationException($"Category with name '{categoryName}' already exists.");
		}
		var category = new Category { Name = categoryName };
		return _categoryRepository.Add(category);
	}

	public Category UpdateCategory(int categoryId, string categoryName)
	{
		var category = _categoryRepository.GetById(categoryId);
		if (category == null)
		{
			throw new KeyNotFoundException($"Category with ID '{categoryId}' not found.");
		}
		category.Name = categoryName;
		return _categoryRepository.Update(category);
	}

	public bool DeleteCategory(int categoryId)
	{
		var category = _categoryRepository.GetById(categoryId);
		if (category == null)
		{
			return false;
		}
		if (category.Products.Any())
		{
			return false;
		}
		return _categoryRepository.Delete(category);;
	}
}