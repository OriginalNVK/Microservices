using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProductService.DTOs;
using ProductService.Kafka;
using ProductService.Models;
using ProductService.Repositories;

namespace ProductService.Services;

public class ProductService : IProductService
{
	private readonly IProductRepository _productRepository;
	private readonly ICategoryRepository _categoryRepository;
	private readonly IMapper _mapper;
	private readonly IKafkaProducer _kafkaProducer;

	public ProductService(
		IProductRepository productRepository,
		ICategoryRepository categoryRepository,
		IMapper mapper,
		IKafkaProducer kafkaProducer)
	{
		_productRepository = productRepository;
		_categoryRepository = categoryRepository;
		_mapper = mapper;
		_kafkaProducer = kafkaProducer;
	}

	public List<ProductResDto> GetAllProducts()
	{
		var entities = _productRepository.GetAll();
		var items = _mapper.Map<List<ProductResDto>>(entities);
		return items;
	}

	public ProductResDto? GetProductById(int productId)
	{
		var entity = _productRepository.GetById(productId);
		return entity == null ? null : _mapper.Map<ProductResDto>(entity);
	}

	public ProductResDto CreateProduct(ProductReqDto productDto)
	{
		if (!_categoryRepository.Query().Any(c => c.Id == productDto.CategoryId))
		{
			return null;
		}
		else if(_productRepository.Query().Any(p => p.Name == productDto.Name && p.CategoryId == productDto.CategoryId))
		{
			return null;
		}

		var product = _mapper.Map<Product>(productDto);
		_productRepository.Add(product);

		return _mapper.Map<ProductResDto>(product);
	}

	public ProductResDto UpdateProduct(int productId, ProductReqDto productDto)
	{
		var product = _productRepository.GetById(productId);
		if (product == null)
		{
			return null;
		}

		if (!_categoryRepository.Query().Any(c => c.Id == productDto.CategoryId))
		{
			return null;
		}
		_mapper.Map(productDto, product);
		_productRepository.Update(product);

		return _mapper.Map<ProductResDto>(product);
	}

	public bool DeleteProduct(int productId)
	{
		var product = _productRepository.GetById(productId);
		if (product == null)
			return false;

		_productRepository.Delete(product);

		return true;
	}
}