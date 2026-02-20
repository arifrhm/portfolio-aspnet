using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Data;

public interface IProductDbContextFactory
{
    ProductDbContext CreateDbContext();
    ProductDbContext CreateDbContextForTenant(Tenant tenant);
}

public class ProductDbContextFactory : IProductDbContextFactory
{
    private readonly IDbContextFactory<ProductDbContext> _baseFactory;
    private readonly IOptions<DatabaseOptions> _options;
    private readonly ILogger<ProductDbContextFactory> _logger;

    public ProductDbContextFactory(
        IDbContextFactory<ProductDbContext> baseFactory,
        IOptions<DatabaseOptions> options,
        ILogger<ProductDbContextFactory> logger)
    {
        _baseFactory = baseFactory;
        _options = options;
        _logger = logger;
    }

    public ProductDbContext CreateDbContext()
    {
        // Default context for tenant management (main database)
        return _baseFactory.CreateDbContext();
    }

    public ProductDbContext CreateDbContextForTenant(Tenant tenant)
    {
        if (tenant.UseDatabasePerTenant)
        {
            // Premium tenant: Use separate database
            return CreateDbContextForDatabase(tenant);
        }
        else
        {
            // Standard tenant: Use schema in main database
            return CreateDbContextForSchema(tenant);
        }
    }

    private ProductDbContext CreateDbContextForDatabase(Tenant tenant)
    {
        _logger.LogInformation("Creating DbContext for tenant {TenantSlug} with database per tenant", tenant.Slug);

        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();
        optionsBuilder.UseSqlServer(tenant.ConnectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        var context = new ProductDbContext(optionsBuilder.Options);
        return context;
    }

    private ProductDbContext CreateDbContextForSchema(Tenant tenant)
    {
        _logger.LogInformation("Creating DbContext for tenant {TenantSlug} with schema: {SchemaName}", tenant.Slug, tenant.SchemaName);

        // For schema-based tenants, use the base factory but with schema name
        var context = _baseFactory.CreateDbContext();

        // The schema is applied in the ProductDbContext constructor
        // We need to create a new instance with schema parameter
        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();
        optionsBuilder.UseSqlServer(_options.Value.DefaultConnection, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        var contextWithSchema = new ProductDbContext(optionsBuilder.Options, tenant.SchemaName);
        return contextWithSchema;
    }
}

public class DatabaseOptions
{
    public const string SectionName = "Database";
    public string DefaultConnection { get; set; } = string.Empty;
}
