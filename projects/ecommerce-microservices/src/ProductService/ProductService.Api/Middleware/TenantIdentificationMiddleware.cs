using ProductService.Domain.Entities;

namespace ProductService.Api.Middleware;

public class TenantIdentificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIdentificationMiddleware> _logger;
    private readonly ITenantService _tenantService;

    public TenantIdentificationMiddleware(
        RequestDelegate next,
        ILogger<TenantIdentificationMiddleware> logger,
        ITenantService tenantService)
    {
        _next = next;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Try to identify tenant from header
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var tenantSlugValues))
        {
            var tenantSlug = tenantSlugValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantSlug))
            {
                var tenant = await _tenantService.GetBySlugAsync(tenantSlug);

                if (tenant != null && tenant.IsActive)
                {
                    tenantContext.SetTenant(tenant.Id, tenant.Slug);
                    _logger.LogInformation("Identified tenant: {TenantSlug} ({TenantId})", tenantSlug, tenant.Id);
                }
                else
                {
                    _logger.LogWarning("Tenant not found or inactive: {TenantSlug}", tenantSlug);
                }
            }
        }
        // Try to identify tenant from subdomain
        else
        {
            var host = context.Request.Host.Host;
            var subdomain = GetSubdomainFromHost(host);

            if (!string.IsNullOrEmpty(subdomain))
            {
                var tenant = await _tenantService.GetBySlugAsync(subdomain);

                if (tenant != null && tenant.IsActive)
                {
                    tenantContext.SetTenant(tenant.Id, tenant.Slug);
                    _logger.LogInformation("Identified tenant from subdomain: {TenantSlug} ({TenantId})", subdomain, tenant.Id);
                }
            }
        }

        if (!tenantContext.HasTenant)
        {
            _logger.LogWarning("No tenant identified for request: {Path}", context.Request.Path);
        }

        await _next(context);
    }

    private string? GetSubdomainFromHost(string host)
    {
        var parts = host.Split('.');

        if (parts.Length >= 3)
        {
            // Return first part as subdomain (e.g., tenant1.api.example.com -> tenant1)
            return parts[0];
        }

        return null;
    }
}

// Interface for tenant service (to be implemented in Application layer)
public interface ITenantService
{
    Task<Tenant?> GetBySlugAsync(string slug);
}

// Mock implementation for now
public class TenantService : ITenantService
{
    private readonly ILogger<TenantService> _logger;

    public TenantService(ILogger<TenantService> logger)
    {
        _logger = logger;
    }

    public async Task<Tenant?> GetBySlugAsync(string slug)
    {
        // In real implementation, this would query the database
        // For demo purposes, we'll return mock data

        await Task.Delay(1); // Simulate async

        var mockTenants = new[]
        {
            new Tenant("Company A", "company-a", "Server=host;Database=company_a_db;..."),
            new Tenant("Company B", "company-b", "Server=host;Database=company_b_db;..."),
            new Tenant("Company C", "company-c", "Server=host;Database=company_c_db;...")
        };

        // Set IDs manually for demo
        foreach (var tenant in mockTenants)
        {
            if (tenant.Slug == slug)
            {
                return tenant;
            }
        }

        return null;
    }
}
