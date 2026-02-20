using FluentAssertions;
using ProductService.Domain.Entities;
using Xunit;

namespace ProductService.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void CreateProduct_WithValidParameters_ShouldCreateProduct()
    {
        // Arrange
        var name = "Laptop Gaming";
        var description = "High-performance gaming laptop";
        var price = 25000000m;
        var stockQuantity = 10;
        var category = "Electronics";
        var sku = "LAP-GAM-001";

        // Act
        var product = new Product(name, description, price, stockQuantity, category, sku);

        // Assert
        product.Should().NotBeNull();
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Price.Should().Be(price);
        product.StockQuantity.Should().Be(stockQuantity);
        product.Category.Should().Be(category);
        product.Sku.Should().Be(sku);
        product.IsActive.Should().BeTrue();
        product.Id.Should().NotBeEmpty();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        product.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(0)]
    public void UpdatePrice_WithInvalidPrice_ShouldThrowArgumentException(decimal invalidPrice)
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        var action = () => product.UpdatePrice(invalidPrice);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Price cannot be negative*");
    }

    [Fact]
    public void UpdatePrice_WithValidPrice_ShouldUpdatePriceAndUpdatedAt()
    {
        // Arrange
        var product = CreateValidProduct();
        var originalPrice = product.Price;
        var newPrice = 30000000m;

        // Act
        product.UpdatePrice(newPrice);

        // Assert
        product.Price.Should().Be(newPrice);
        product.Price.Should().NotBe(originalPrice);
        product.UpdatedAt.Should().NotBeNull();
        product.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(0)]
    public void UpdateStock_WithInvalidQuantity_ShouldThrowArgumentException(int invalidQuantity)
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        var action = () => product.UpdateStock(invalidQuantity);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Stock quantity cannot be negative*");
    }

    [Fact]
    public void UpdateStock_WithValidQuantity_ShouldUpdateStockAndUpdatedAt()
    {
        // Arrange
        var product = CreateValidProduct();
        var originalStock = product.StockQuantity;
        var newStock = 50;

        // Act
        product.UpdateStock(newStock);

        // Assert
        product.StockQuantity.Should().Be(newStock);
        product.StockQuantity.Should().NotBe(originalStock);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrueAndUpdateUpdatedAt()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Deactivate();
        product.IsActive.Should().BeFalse();

        // Act
        product.Activate();

        // Assert
        product.IsActive.Should().BeTrue();
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalseAndUpdateUpdatedAt()
    {
        // Arrange
        var product = CreateValidProduct();
        product.IsActive.Should().BeTrue();

        // Act
        product.Deactivate();

        // Assert
        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(10, 10, true)]
    [InlineData(10, 11, false)]
    [InlineData(10, 100, false)]
    public void IsInStock_WithVariousQuantities_ShouldReturnCorrectResult(int stock, int requested, bool expectedResult)
    {
        // Arrange
        var product = new Product(
            "Test Product",
            "Test Description",
            1000m,
            stock,
            "Test Category",
            "TEST-001"
        );

        // Act
        var result = product.IsInStock(requested);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void IsInStock_WhenProductIsInactive_ShouldReturnFalse()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Deactivate();

        // Act
        var result = product.IsInStock(5);

        // Assert
        result.Should().BeFalse();
    }

    private Product CreateValidProduct()
    {
        return new Product(
            "Test Product",
            "Test Description",
            10000m,
            10,
            "Test Category",
            "TEST-001"
        );
    }
}
