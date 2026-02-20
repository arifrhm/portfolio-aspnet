namespace ProductService.Domain.Entities;

public class TenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantSlug { get; private set; }

    public TenantContext(Guid? tenantId, string? tenantSlug)
    {
        TenantId = tenantId;
        TenantSlug = tenantSlug;
    }

    public static TenantContext Empty => new(null, null);

    public void SetTenant(Guid tenantId, string tenantSlug)
    {
        TenantId = tenantId;
        TenantSlug = tenantSlug;
    }

    public bool HasTenant => TenantId.HasValue && !string.IsNullOrEmpty(TenantSlug);
}
