using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Plugins.Network.Ping;
using System.Net;

namespace LoadTests;

public class ProductLoadTests
{
    private const string BaseUrl = "http://localhost:5000";
    private const string ApiUrl = $"{BaseUrl}/api/products";

    public static void Main(string[] args)
    {
        Console.WriteLine("=== Product Service Load Tests ===");
        Console.WriteLine($"Target: {BaseUrl}\n");

        // Run individual scenarios
        Console.WriteLine("1. Running GET All Products Load Test...");
        GetAllProductsScenario().Run();

        Console.WriteLine("\n2. Running GET Product by ID Load Test...");
        GetProductByIdScenario().Run();

        Console.WriteLine("\n3. Running POST Create Product Load Test...");
        CreateProductScenario().Run();

        Console.WriteLine("\n4. Running Multi-Tenant Load Test...");
        MultiTenantScenario().Run();

        Console.WriteLine("\n5. Running Stress Test...");
        StressTestScenario().Run();

        Console.WriteLine("\n=== All Load Tests Completed ===");
    }

    // Scenario 1: Get All Products (Read-Heavy)
    private static Scenario GetAllProductsScenario()
    {
        var step = HttpStep.Create("get_all_products", context =>
            Http.CreateRequest("GET", ApiUrl)
                .WithHeader("Accept", "application/json")
                .WithHeader("X-Tenant-Slug", "company-a")
        );

        return ScenarioBuilder
            .CreateScenario("Get All Products", step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(30))
            );
    }

    // Scenario 2: Get Product by ID
    private static Scenario GetProductByIdScenario()
    {
        var productIds = Enumerable.Range(1, 100).Select(i => Guid.NewGuid()).ToList();
        var random = new Random();

        var step = HttpStep.Create("get_product_by_id", context =>
            Http.CreateRequest($"{ApiUrl}/{productIds[random.Next(productIds.Count)]}")
                .WithHeader("Accept", "application/json")
                .WithHeader("X-Tenant-Slug", "company-b")
        );

        return ScenarioBuilder
            .CreateScenario("Get Product by ID", step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30))
            );
    }

    // Scenario 3: Create Product (Write Operations)
    private static Scenario CreateProductScenario()
    {
        var step = HttpStep.Create("create_product", context =>
        {
            var product = new
            {
                name = $"Test Product {Guid.NewGuid()}",
                description = "Load test product description",
                price = 10000m,
                stockQuantity = 50,
                category = "Electronics",
                sku = $"SKU-{Guid.NewGuid():N}"
            };

            return Http.CreateRequest("POST", ApiUrl)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Accept", "application/json")
                .WithHeader("X-Tenant-Slug", "company-c")
                .WithBody(System.Text.Json.JsonSerializer.Serialize(product));
        });

        return ScenarioBuilder
            .CreateScenario("Create Product", step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.RampingConstant(copies: 20, during: TimeSpan.FromMinutes(1))
            );
    }

    // Scenario 4: Multi-Tenant Load Test
    private static Scenario MultiTenantScenario()
    {
        var tenants = new[] { "company-a", "company-b", "company-c" };
        var random = new Random();

        var step = HttpStep.Create("multi_tenant_get_products", context =>
        {
            var tenant = tenants[random.Next(tenants.Length)];
            return Http.CreateRequest(ApiUrl)
                .WithHeader("Accept", "application/json")
                .WithHeader("X-Tenant-Slug", tenant);
        });

        return ScenarioBuilder
            .CreateScenario("Multi-Tenant Load", step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 30, during: TimeSpan.FromSeconds(30))
            );
    }

    // Scenario 5: Stress Test (Increasing Load)
    private static Scenario StressTestScenario()
    {
        var step = HttpStep.Create("stress_test_get", context =>
            Http.CreateRequest(ApiUrl)
                .WithHeader("Accept", "application/json")
                .WithHeader("X-Tenant-Slug", "company-a")
        );

        return ScenarioBuilder
            .CreateScenario("Stress Test", step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: 100, during: TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: 200, during: TimeSpan.FromSeconds(30))
            );
    }

    // Helper method to run scenarios
    private static void Run(this Scenario scenario)
    {
        var pingPlugin = PingPlugin.Create();

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(pingPlugin)
            .WithTestSuite("Product Service Load Tests")
            .WithTestName($"Test - {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .Run();
    }
}
