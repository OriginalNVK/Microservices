using ProductService.DTOs;

namespace ProductService.Services;

public interface IProductService
{
	List<ProductResDto> GetAllProducts();
	ProductResDto? GetProductById(int productId);
	ProductResDto CreateProduct(ProductReqDto productDto);
	ProductResDto UpdateProduct(int productId, ProductReqDto productDto);
	bool DeleteProduct(int productId);
}
