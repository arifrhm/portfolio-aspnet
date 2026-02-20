# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ProductService.sln ./
COPY ProductService.Api/ProductService.Api.csproj ./ProductService.Api/
COPY ProductService.Domain/ProductService.Domain.csproj ./ProductService.Domain/
COPY ProductService.Application/ProductService.Application.csproj ./ProductService.Application/
COPY ProductService.Infrastructure/ProductService.Infrastructure.csproj ./ProductService.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/ProductService.Api
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install ICU libraries for globalization
RUN apt-get update && apt-get install -y libicu-dev && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 80
EXPOSE 443

# Set entrypoint
ENTRYPOINT ["dotnet", "ProductService.Api.dll"]
