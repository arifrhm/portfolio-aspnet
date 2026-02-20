using ProductService.Application.DTOs;

namespace ProductService.Application.Interfaces;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<ProductListDto> GetAllAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ProductListDto> GetByCategoryAsync(string category, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductDto productDto, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto productDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CheckStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
