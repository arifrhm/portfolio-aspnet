using AutoMapper;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching product with ID: {ProductId}", id);

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        return product is null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching product with SKU: {Sku}", sku);

        var product = await _productRepository.GetBySkuAsync(sku, cancellationToken);

        return product is null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductListDto> GetAllAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching all products - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var products = (await _productRepository.GetAllAsync(cancellationToken))
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var totalCount = await _productRepository.CountAsync(cancellationToken);

        return new ProductListDto(
            _mapper.Map<IEnumerable<ProductDto>>(products),
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<ProductListDto> GetByCategoryAsync(string category, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching products in category: {Category}", category);

        var products = (await _productRepository.GetByCategoryAsync(category, cancellationToken))
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var totalCount = await _productRepository.CountAsync(cancellationToken);

        return new ProductListDto(
            _mapper.Map<IEnumerable<ProductDto>>(products),
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto productDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new product: {ProductName}", productDto.Name);

        // Check if SKU already exists
        var existingProduct = await _productRepository.GetBySkuAsync(productDto.Sku, cancellationToken);
        if (existingProduct is not null)
        {
            throw new InvalidOperationException($"Product with SKU '{productDto.Sku}' already exists");
        }

        var product = new Product(
            productDto.Name,
            productDto.Description,
            productDto.Price,
            productDto.StockQuantity,
            productDto.Category,
            productDto.Sku
        );

        await _productRepository.AddAsync(product, cancellationToken);

        _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto productDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", id);

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        // Update price if changed
        if (product.Price != productDto.Price)
        {
            product.UpdatePrice(productDto.Price);
        }

        // Update stock if changed
        if (product.StockQuantity != productDto.StockQuantity)
        {
            product.UpdateStock(productDto.StockQuantity);
        }

        // Update active status
        if (product.IsActive != productDto.IsActive)
        {
            if (productDto.IsActive)
                product.Activate();
            else
                product.Deactivate();
        }

        await _productRepository.UpdateAsync(product, cancellationToken);

        _logger.LogInformation("Product updated successfully: {ProductId}", id);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);

        var exists = await _productRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            return false;
        }

        await _productRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Product deleted successfully: {ProductId}", id);

        return true;
    }

    public async Task<bool> CheckStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking stock for product {ProductId} - Requested: {Quantity}", productId, quantity);

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            _logger.LogWarning("Product not found: {ProductId}", productId);
            return false;
        }

        var inStock = product.IsInStock(quantity);

        _logger.LogInformation("Stock check result for {ProductId}: {InStock}", productId, inStock);

        return inStock;
    }
}
