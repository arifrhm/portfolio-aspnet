using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IProductDbContextFactory _dbContextFactory;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(
        IProductDbContextFactory dbContextFactory,
        TenantContext tenantContext,
        ILogger<ProductRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private ProductDbContext CreateDbContext()
    {
        // For now, we'll use the default context
        // In a real implementation, we'd get the tenant from context
        // and create the appropriate DbContext
        var context = _dbContextFactory.CreateDbContext();

        if (_tenantContext.HasTenant)
        {
            context.CurrentTenantId = _tenantContext.TenantId;
        }

        return context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();
        return await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();
        return await context.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();
        return await context.Products.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();
        return await context.Products
            .Where(p => p.Category == category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();
        return await context.Products
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();

        // Ensure tenant context is set
        if (_tenantContext.HasTenant)
        {
            context.CurrentTenantId = _tenantContext.TenantId;
            product = new Product(
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.Category,
                product.Sku,
                _tenantContext.TenantId!.Value);
        }

        await context.Products.AddAsync(product, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added product {ProductId} for tenant {TenantId}", product.Id, product.TenantId);

        return product;
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();

        context.Products.Update(product);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated product {ProductId} for tenant {TenantId}", product.Id, product.TenantId);

        return product;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product != null)
        {
            context.Products.Remove(product);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted product {ProductId} for tenant {TenantId}", id, product.TenantId);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();
        return await context.Products.AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext();
        return await context.Products.CountAsync(cancellationToken);
    }
}
