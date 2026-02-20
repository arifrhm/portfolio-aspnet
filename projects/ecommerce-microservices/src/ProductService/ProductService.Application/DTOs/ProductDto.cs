namespace ProductService.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category,
    string Sku,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateProductDto(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category,
    string Sku
);

public record UpdateProductDto(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category,
    bool IsActive
);

public record ProductListDto(
    IEnumerable<ProductDto> Products,
    int TotalCount,
    int Page,
    int PageSize
);
