# Load Testing

This directory contains load testing scripts for the Product Service API.

## Prerequisites

### NBomber (.NET)
```bash
cd tests/LoadTests
dotnet restore
```

### k6 (JavaScript)
Download and install k6 from: https://k6.io/docs/getting-started/installation/

### Python
```bash
pip install aiohttp
```

## Running Load Tests

### Option 1: NBomber (Recommended for .NET projects)

NBomber is a modern, flexible load testing framework for .NET.

```bash
# Make sure the API is running
cd src/ProductService
dotnet run

# In another terminal, run load tests
cd tests/LoadTests
dotnet run
```

**Scenarios included:**
1. **Get All Products** - Read-heavy test (10 concurrent users, 30s)
2. **Get Product by ID** - Random product lookup (50 concurrent users, 30s)
3. **Create Product** - Write operations test (20 concurrent users, 1m ramp-up)
4. **Multi-Tenant Load** - Simulate multiple tenants (30 concurrent users, 30s)
5. **Stress Test** - Increasing load (10 → 50 → 100 → 200 concurrent users)

**Reports:**
NBomber generates HTML reports in the `reports` folder.

### Option 2: k6 (JavaScript)

k6 is a modern load testing tool with a clean API.

```bash
# Make sure the API is running
cd src/ProductService
dotnet run

# In another terminal, run k6
k6 run load-tests/k6-load-test.js
```

**Test Profile:**
- Ramp up: 10 → 50 → 100 users
- Sustain: 100 users for 3 minutes
- Ramp down: 100 → 50 → 10 → 0 users
- Total duration: ~10 minutes

**Thresholds:**
- 95% of requests must complete below 500ms
- Error rate must be less than 5%

### Option 3: Python Load Test

Simple Python script using asyncio and aiohttp.

```bash
# Make sure the API is running
cd src/ProductService
dotnet run

# Run Python load test
python3 load-tests/python-load-test.py
```

**Scenarios included:**
1. Low Concurrency Test - 10 concurrent users, 100 requests
2. Medium Concurrency Test - 50 concurrent users, 500 requests
3. High Concurrency Test - 100 concurrent users, 1,000 requests
4. Stress Test - 200 concurrent users, 2,000 requests

## Multi-Tenant Load Testing

All load test scripts support multi-tenant scenarios:

- **Tenants:** company-a, company-b, company-c
- **Identification:** Via `X-Tenant-Slug` header
- **Distribution:** Requests distributed across all tenants

### Tenant Identification in Load Tests

**NBomber:**
```csharp
.WithHeader("X-Tenant-Slug", "company-a")
```

**k6:**
```javascript
headers: {
  'X-Tenant-Slug': tenant
}
```

**Python:**
```python
headers = {
    "X-Tenant-Slug": tenant
}
```

## Interpreting Results

### Key Metrics

| Metric | Description | Good | Warning | Critical |
|--------|-------------|------|----------|----------|
| Success Rate | % of successful requests | >99% | 95-99% | <95% |
| Avg Response Time | Mean response time | <100ms | 100-200ms | >200ms |
| P95 Response Time | 95th percentile | <200ms | 200-500ms | >500ms |
| P99 Response Time | 99th percentile | <500ms | 500-1000ms | >1000ms |
| Requests/sec | Throughput | High | Medium | Low |
| Error Rate | % of failed requests | <1% | 1-5% | >5% |

### Performance Targets for Product Service

Based on the portfolio metrics:

- **Daily Capacity:** 100K+ requests
- **Target Response Time:** Sub-50ms average
- **Uptime:** 99.9%
- **Concurrency:** 10K+ connections

### Sample Analysis

```
Test: High Concurrency Test
Total Requests: 1000
Successful: 985
Failed: 15
Success Rate: 98.5% ⚠️ (below 99% target)

Response Times (successful requests):
  Average: 145ms ⚠️ (above 100ms target)
  Median (p50): 120ms
  95th percentile: 250ms ⚠️ (above 200ms target)
  Min: 25ms
  Max: 850ms ❌ (very high spike)

Recommendations:
1. Add Redis caching for frequently accessed products
2. Optimize database queries (add indexes)
3. Increase Kubernetes replica count
4. Review slow queries in Application Insights
```

## Customizing Load Tests

### Changing Target URL

**NBomber:**
```csharp
private const string BaseUrl = "http://your-url:5000";
```

**k6:**
```javascript
const BASE_URL = 'http://your-url:5000';
```

**Python:**
```python
runner = LoadTestRunner(base_url="http://your-url:5000")
```

### Changing Concurrency and Duration

**NBomber:**
```csharp
.WithLoadSimulations(
    Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(5))
)
```

**k6:**
```javascript
stages: [
  { duration: '5m', target: 100 },
]
```

**Python:**
```python
await runner.run_scenario(
    "Custom Test",
    num_requests=5000,
    concurrency=100
)
```

## CI/CD Integration

### GitHub Actions

Add to `.github/workflows/load-tests.yml`:

```yaml
name: Load Tests

on:
  schedule:
    - cron: '0 0 * * *'  # Daily at midnight
  workflow_dispatch:

jobs:
  load-test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Start API
      run: |
        cd src/ProductService
        dotnet run &
        sleep 30

    - name: Run NBomber
      run: |
        cd tests/LoadTests
        dotnet run

    - name: Upload Reports
      uses: actions/upload-artifact@v3
      with:
        name: load-test-reports
        path: tests/LoadTests/reports/
```

## Troubleshooting

### Connection Refused
- Make sure the API is running: `cd src/ProductService && dotnet run`
- Check the port: Default is 5000

### High Error Rate
- Check API logs: `tail -f logs/productservice-*.txt`
- Verify database connection
- Check rate limiting configuration

### Slow Response Times
- Enable Redis caching
- Check database query performance
- Review Kubernetes HPA and add more replicas
- Check CPU/memory usage with `kubectl top pods`

### Out of Memory
- Increase container memory limits
- Reduce concurrent users in load test
- Check for memory leaks in application

## Additional Resources

- [NBomber Documentation](https://nbomber.com/)
- [k6 Documentation](https://k6.io/docs/)
- [Load Testing Best Practices](https://k6.io/docs/guides/test-creation/)
- [Application Performance Monitoring](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

*Remember: Load testing helps identify bottlenecks before they impact production users.*
