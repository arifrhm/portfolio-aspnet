# Multi-Tenant Architecture Documentation

## Overview

The Product Service implements a flexible multi-tenant architecture supporting both **schema-based isolation** (standard tenants) and **database-based isolation** (premium tenants).

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway / Load Balancer             │
└────────────────────────────┬────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Client A    │     │  Client B    │     │  Client C    │
│ (Standard)   │     │ (Standard)   │     │ (Premium)    │
│ company-a    │     │ company-b    │     │ company-c    │
└──────────────┘     └──────────────┘     └──────────────┘
        │                    │                    │
        └────────────────────┼────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  API Service    │
                    └────────┬───────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Schema A    │     │  Schema B    │     │   Database   │
│ (company_a)  │     │ (company_b)  │     │  company-c   │
│  Tables      │     │  Tables      │     │  Tables      │
└──────────────┘     └──────────────┘     └──────────────┘
        │                    │                    │
        └────────────────────┼────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  Shared DB     │
                    │  (Schema A+B)  │
                    └────────────────┘
```

## Tenant Isolation Strategies

### 1. Schema-Based Isolation (Standard Tenants)

**Used for:** Standard-tier tenants

**How it works:**
- Single database
- Separate schema for each tenant
- Schema name = tenant slug
- `company-a.products`, `company-b.products`, etc.

**Benefits:**
- Lower cost (shared database)
- Easier management
- Good for small to medium-sized tenants

**Trade-offs:**
- Potential resource contention
- Limited isolation at storage level
- Backup/restore affects all tenants in same DB

**Database Structure:**
```sql
-- Main database has schemas for each tenant
CREATE SCHEMA company_a;
CREATE SCHEMA company_b;
CREATE SCHEMA company_c;

-- Each schema has its own tables
company_a.products
company_b.products
company_c.products
```

### 2. Database-Based Isolation (Premium Tenants)

**Used for:** Premium-tier tenants

**How it works:**
- Separate database per tenant
- Dedicated connection string
- Full isolation of data, resources, and backups

**Benefits:**
- Complete isolation
- Independent scaling
- Separate backups and restores
- Better performance for high-volume tenants
- Can migrate to different servers

**Trade-offs:**
- Higher cost
- More complex management
- Connection pool overhead

**Database Structure:**
```
server1/
├── company_a.db
├── company_b.db
└── company_c.db (premium)
```

## Tenant Identification

### Methods

#### 1. Header-Based Identification

```http
GET /api/products
X-Tenant-Slug: company-a
```

**Pros:**
- Simple to implement
- Works with any domain
- Easy to test

**Cons:**
- Header must be sent with every request
- Requires client cooperation

#### 2. Subdomain-Based Identification

```
http://company-a.api.example.com/api/products
http://company-b.api.example.com/api/products
```

**Pros:**
- Automatic identification
- Clean URLs
- Works with SSL/TLS

**Cons:**
- Requires DNS configuration
- Wildcard SSL certificate needed

**Middleware Implementation:**
```csharp
// Extracts tenant from subdomain
var subdomain = GetSubdomainFromHost(context.Request.Host.Host);
```

## Implementation Details

### Tenant Entity

```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }           // URL-safe identifier
    public string ConnectionString { get; set; } // For premium tenants
    public string SchemaName { get; set; }       // For standard tenants
    public TenantPlan Plan { get; set; }        // Standard or Premium
    public bool IsActive { get; set; }
}
```

### Tenant Context

```csharp
public class TenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantSlug { get; private set; }

    public bool HasTenant => TenantId.HasValue;

    public void SetTenant(Guid tenantId, string tenantSlug)
    {
        TenantId = tenantId;
        TenantSlug = tenantSlug;
    }
}
```

### DbContext Factory

```csharp
public interface IProductDbContextFactory
{
    ProductDbContext CreateDbContext();                    // Main DB (tenant mgmt)
    ProductDbContext CreateDbContextForTenant(Tenant tenant); // Tenant-specific DB
}

public class ProductDbContextFactory : IProductDbContextFactory
{
    public ProductDbContext CreateDbContextForTenant(Tenant tenant)
    {
        if (tenant.UseDatabasePerTenant)
        {
            // Premium: Separate database
            return CreateDbContextForDatabase(tenant);
        }
        else
        {
            // Standard: Schema-based isolation
            return CreateDbContextForSchema(tenant);
        }
    }
}
```

### Tenant Identification Middleware

```csharp
public class TenantIdentificationMiddleware
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Try header first
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var tenantSlug))
        {
            var tenant = await _tenantService.GetBySlugAsync(tenantSlug);
            if (tenant != null && tenant.IsActive)
            {
                tenantContext.SetTenant(tenant.Id, tenant.Slug);
            }
        }

        // Fallback to subdomain
        else
        {
            var host = context.Request.Host.Host;
            var subdomain = GetSubdomainFromHost(host);
            var tenant = await _tenantService.GetBySlugAsync(subdomain);
            if (tenant != null && tenant.IsActive)
            {
                tenantContext.SetTenant(tenant.Id, tenant.Slug);
            }
        }

        await _next(context);
    }
}
```

### Global Query Filters

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        entity.HasQueryFilter(p => p.TenantId == CurrentTenantId);
    });
}
```

### Automatic TenantId Assignment

```csharp
public override int SaveChanges()
{
    var entries = ChangeTracker.Entries<Product>()
        .Where(e => e.State == EntityState.Added);

    foreach (var entry in entries)
    {
        entry.Entity.TenantId = CurrentTenantId.Value;
    }

    return base.SaveChanges();
}
```

## Data Isolation

### Query Isolation

All queries automatically filtered by `TenantId`:

```csharp
// Without specifying tenant, automatically filters by current tenant
var products = await _repository.GetAllAsync();

// Generates SQL: SELECT * FROM products WHERE tenant_id = @currentTenantId
```

### Write Isolation

New entities automatically assigned `TenantId`:

```csharp
// TenantId is automatically set from current tenant context
var product = new Product(...);
await _repository.AddAsync(product);
```

### Indexing

```csharp
// TenantId indexed for query performance
modelBuilder.Entity<Product>()
    .HasIndex(e => e.TenantId);

// Unique constraint on SKU per tenant
modelBuilder.Entity<Product>()
    .HasIndex(e => new { e.TenantId, e.Sku })
    .IsUnique();
```

## Security Considerations

### Tenant Isolation Guarantees

✅ **Query Isolation:** All queries automatically filter by tenant
✅ **Write Protection:** Cannot create entities without tenant context
✅ **Schema Separation:** Standard tenants cannot access each other's schemas
✅ **Database Separation:** Premium tenants have separate databases
✅ **Connection Isolation:** Each tenant uses separate connection pool

### Preventing Cross-Tenant Access

```csharp
// Attempt to query another tenant's products
var allProducts = await context.Products.ToListAsync();
// Still filtered by current tenant!

// Attempt to create product without tenant context
var product = new Product(...);
product.TenantId = otherTenantId; // ❌ Overridden by SaveChanges
await context.SaveChangesAsync(); // Sets to current tenant ID
```

### Authorization

Combine tenant isolation with role-based access:

```csharp
[HttpGet]
[Authorize(Roles = "Admin,ProductManager")]
public async Task<ActionResult<ProductDto>> GetById(Guid id)
{
    // Already filtered by tenant context
    var product = await _productService.GetByIdAsync(id);
    return Ok(product);
}
```

## Performance Optimization

### Connection Pooling

```csharp
// Separate connection pools per database
services.AddDbContextFactory<ProductDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(5);
    });
});
```

### Caching Strategy

```csharp
// Cache keys include tenant slug
var cacheKey = $"product:{tenantSlug}:{productId}";

var cachedProduct = await _cache.GetAsync<Product>(cacheKey);
if (cachedProduct != null)
{
    return cachedProduct;
}

// Fetch from database
var product = await _repository.GetByIdAsync(productId);

// Cache with tenant-specific key
await _cache.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5));
```

### Database Indexing

```csharp
// Compound index for tenant-filtered queries
modelBuilder.Entity<Product>()
    .HasIndex(e => new { e.TenantId, e.Category, e.IsActive });

// Covering index for common queries
modelBuilder.Entity<Product>()
    .HasIndex(e => new { e.TenantId, e.Sku, e.IsActive })
    .IncludeProperties(e => new { e.Name, e.Price });
```

## Migration Strategy

### Schema-Based Migrations

```bash
# Generate migration for main database
dotnet ef migrations add InitialCreate

# Apply to main database
dotnet ef database update

# Manually create schemas for tenants
CREATE SCHEMA company_a;
CREATE SCHEMA company_b;

# Apply migrations to each schema
# (Need to modify migration script or use EF Core schema parameter)
```

### Database-Based Migrations

```bash
# Generate migration
dotnet ef migrations add InitialCreate

# Apply to each tenant database
dotnet ef database update --connection "Server=host;Database=company_a_db;..."
dotnet ef database update --connection "Server=host;Database=company_b_db;..."
dotnet ef database update --connection "Server=host;Database=company_c_db;..."
```

### Automated Schema Migration

```csharp
public async Task MigrateTenantSchemaAsync(Tenant tenant)
{
    if (tenant.UseDatabasePerTenant)
    {
        var context = _dbContextFactory.CreateDbContextForDatabase(tenant);
        await context.Database.MigrateAsync();
    }
    else
    {
        var context = _dbContextFactory.CreateDbContextForSchema(tenant);
        await context.Database.MigrateAsync();
    }
}
```

## Monitoring & Observability

### Tenant-Aware Logging

```csharp
_logger.LogInformation(
    "Processing request for tenant {TenantSlug} ({TenantId})",
    tenantContext.TenantSlug,
    tenantContext.TenantId
);
```

### Metrics by Tenant

```csharp
// Track request count per tenant
_metrics.Counter("http_requests")
    .Tag("tenant", tenantContext.TenantSlug)
    .Tag("endpoint", endpoint)
    .Increment();

// Track response time per tenant
_metrics.Histogram("http_request_duration")
    .Tag("tenant", tenantContext.TenantSlug)
    .Record(responseTimeMs);
```

### Health Checks

```csharp
// Health check per tenant
app.MapGet("/health/{tenantSlug}", async (string tenantSlug) =>
{
    var tenant = await _tenantService.GetBySlugAsync(tenantSlug);
    if (tenant == null)
        return Results.NotFound();

    var context = _dbContextFactory.CreateDbContextForTenant(tenant);
    var canConnect = await context.Database.CanConnectAsync();

    return canConnect ? Results.Ok() : Results.StatusCode(503);
});
```

## Testing

### Unit Tests

```csharp
[Fact]
public async Task GetAllProducts_ShouldOnlyReturnCurrentTenantProducts()
{
    // Arrange
    var tenantA = CreateTenant("company-a");
    var tenantB = CreateTenant("company-b");

    var productA = new Product(..., tenantA.Id);
    var productB = new Product(..., tenantB.Id);

    await _repository.AddAsync(productA);
    await _repository.AddAsync(productB);

    _tenantContext.SetTenant(tenantA.Id, "company-a");

    // Act
    var products = await _repository.GetAllAsync();

    // Assert
    products.Should().ContainSingle(p => p.TenantId == tenantA.Id);
    products.Should().NotContain(p => p.TenantId == tenantB.Id);
}
```

### Load Testing

See `load-tests/README.md` for multi-tenant load testing strategies.

## Best Practices

### ✅ Do

1. **Always validate tenant context** before queries
2. **Use global query filters** for automatic isolation
3. **Index TenantId columns** for query performance
4. **Monitor per-tenant metrics** to identify resource hogs
5. **Implement tenant-specific rate limiting**
6. **Cache with tenant-specific keys**
7. **Separate connection pools** per database
8. **Document tenant migration** procedures

### ❌ Don't

1. **Never allow cross-tenant queries**
2. **Don't cache without tenant key prefix**
3. **Avoid shared global state between tenants**
4. **Don't use shared sequences** (use GUIDs instead)
5. **Never bypass tenant context** for admin queries
6. **Don't mix standard/premium tenant data** in same DB

## Migration from Single-Tenant

If migrating an existing single-tenant application:

1. **Create Tenant entity** and add `TenantId` to all entities
2. **Create default tenant** and assign all existing data
3. **Add global query filters** (disabled during migration)
4. **Backfill TenantId** for existing data
5. **Enable query filters**
6. **Deploy with schema isolation** (easier transition)
7. **Move high-volume tenants** to separate databases

---

*This architecture supports both small and large tenants with appropriate isolation levels and costs.*
