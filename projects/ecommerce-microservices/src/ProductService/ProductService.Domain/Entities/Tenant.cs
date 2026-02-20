namespace ProductService.Domain.Entities;

public enum TenantPlan
{
    Standard,  // Schema-based isolation
    Premium    // Database-based isolation
}

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string ConnectionString { get; private set; }
    public string SchemaName { get; private set; }
    public TenantPlan Plan { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // For EF Core
    private Tenant() { }

    public Tenant(
        string name,
        string slug,
        string connectionString,
        string schemaName,
        TenantPlan plan)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        ConnectionString = connectionString;
        SchemaName = schemaName;
        Plan = plan;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public bool UseDatabasePerTenant => Plan == TenantPlan.Premium;
    public bool UseSchemaPerTenant => Plan == TenantPlan.Standard;

    public void UpdatePlan(TenantPlan newPlan)
    {
        Plan = newPlan;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateConnectionString(string newConnectionString)
    {
        ConnectionString = newConnectionString;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string newName)
    {
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
