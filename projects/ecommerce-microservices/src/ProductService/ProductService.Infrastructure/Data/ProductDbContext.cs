using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Data;

public class ProductDbContext : DbContext
{
    private readonly string? _schemaName;

    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public ProductDbContext(DbContextOptions<ProductDbContext> options, string schemaName)
        : base(options)
    {
        _schemaName = schemaName;
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply schema for standard tenants
        if (!string.IsNullOrEmpty(_schemaName))
        {
            modelBuilder.HasDefaultSchema(_schemaName);
        }

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.StockQuantity)
                .IsRequired();

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Sku)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.TenantId)
                .IsRequired();

            // Create index on TenantId for filtering
            entity.HasIndex(e => e.TenantId);

            // Create unique constraint on SKU per tenant
            entity.HasIndex(e => new { e.TenantId, e.Sku })
                .IsUnique();

            // Global query filter for tenant isolation
            entity.HasQueryFilter(p => p.TenantId == CurrentTenantId);

            // Configure navigation property
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Tenant entity (only in main database)
        if (string.IsNullOrEmpty(_schemaName))
        {
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Slug)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ConnectionString)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.SchemaName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Plan)
                    .IsRequired();

                // Create unique constraint on Slug
                entity.HasIndex(e => e.Slug)
                    .IsUnique();
            });
        }
    }

    // Tenant ID setter for global query filter
    public Guid? CurrentTenantId { get; set; }

    // Override SaveChanges to automatically set TenantId
    public override int SaveChanges()
    {
        ApplyTenantIdToEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantIdToEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantIdToEntities()
    {
        var entries = ChangeTracker.Entries<Product>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (CurrentTenantId.HasValue)
            {
                entry.Entity.TenantId = CurrentTenantId.Value;
            }
            else
            {
                throw new InvalidOperationException("Cannot create entity without tenant context");
            }
        }
    }
}
