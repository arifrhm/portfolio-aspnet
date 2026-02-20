## 🚀 Running the Project

### Prerequisites

- .NET 8 SDK
- Docker & Docker Compose
- Git
- (Optional) Kubernetes cluster for K8s deployment
- (Optional) k6 for load testing

### Option 1: Docker Compose (Recommended)

```bash
# Clone repository
git clone https://github.com/arifrhm/portfolio-aspnet.git
cd portfolio-aspnet/projects/ecommerce-microservices/src/ProductService

# Start all services (SQL Server, Redis, API)
docker-compose up -d

# View logs
docker-compose logs -f product-service-api

# Access Swagger UI
open http://localhost:5000
```

### Option 2: Local Development

```bash
# Navigate to project
cd portfolio-aspnet/projects/ecommerce-microservices/src/ProductService

# Restore dependencies
dotnet restore

# Run API
dotnet run --project ProductService.Api

# Access Swagger UI at http://localhost:5000
```

### Stopping the Project

```bash
# Stop Docker Compose
docker-compose down

# Stop local .NET API
# Press Ctrl+C in terminal
```

---

## 📁 Project Structure

```
portfolio-aspnet/
├── projects/
│   ├── ecommerce-microservices/     # Main project
│   │   ├── src/ProductService/     # Product service implementation
│   │   │   ├── ProductService.Api/        # REST API
│   │   │   ├── ProductService.Application/ # Business logic
│   │   │   ├── ProductService.Domain/      # Domain models
│   │   │   └── ProductService.Infrastructure/ # Data access
│   │   ├── tests/                # Unit tests
│   │   ├── k8s/                   # Kubernetes manifests
│   │   ├── load-tests/            # Load testing scripts
│   │   ├── README.md              # Project documentation
│   │   ├── ARCHITECTURE.md        # System architecture
│   │   ├── MULTI_TENANT_ARCHITECTURE.md  # Multi-tenant design
│   │   └── IMPLEMENTATION_SUMMARY.md       # What was implemented
│   └── api-gateway/               # API Gateway project
│       └── README.md
└── README.md                     # This file
```

---

## 🎯 API Endpoints

Once the project is running, access Swagger UI at `http://localhost:5000`

### Multi-Tenant Access

**Using Header:**
```bash
curl http://localhost:5000/api/products \
  -H "X-Tenant-Slug: company-a"
```

**Using Subdomain:**
```bash
curl http://company-a.localhost:5000/api/products
```

**Available Tenants:** `company-a`, `company-b`, `company-c`

### Key Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all products (paginated) |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create new product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Delete product |
| GET | `/api/products/category/{category}` | Get products by category |
| GET | `/health` | Health check |

---

## 🧪 Running Tests

### Unit Tests

```bash
cd projects/ecommerce-microservices/src/ProductService

# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Load Tests

**Option 1: NBomber (.NET)**
```bash
cd tests/LoadTests
dotnet restore
dotnet run

# HTML reports generated in reports/ directory
```

**Option 2: k6 (JavaScript)**
```bash
# Install k6 first
k6 run load-tests/k6-load-test.js
```

**Option 3: Python**
```bash
pip install aiohttp
python3 load-tests/python-load-test.py
```

---

## 🐳 Docker Deployment

### Build Image

```bash
cd projects/ecommerce-microservices/src/ProductService

docker build -t product-service:latest .
```

### Run Container

```bash
docker run -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=host;Database=ProductServiceDb;..." \
  product-service:latest
```

---

## ☸️ Kubernetes Deployment

```bash
# Apply Kubernetes manifests
cd projects/ecommerce-microservices
kubectl apply -f k8s/deployment.yaml

# Check pods
kubectl get pods -l app=product-service

# View logs
kubectl logs -l app=product-service -f

# Port forward (for local testing)
kubectl port-forward svc/product-service 5000:80
```

---

## 📚 Documentation

| Document | Description |
|-----------|-------------|
| [E-Commerce README](projects/ecommerce-microservices/README.md) | Project overview and setup |
| [Architecture](projects/ecommerce-microservices/ARCHITECTURE.md) | System architecture design |
| [Multi-Tenant Architecture](projects/ecommerce-microservices/MULTI_TENANT_ARCHITECTURE.md) | Tenant isolation strategies |
| [Load Testing](projects/ecommerce-microservices/load-tests/README.md) | How to run load tests |
| [API Gateway](projects/api-gateway/README.md) | API Gateway overview |
| [Implementation Summary](projects/ecommerce-microservices/IMPLEMENTATION_SUMMARY.md) | What was implemented |

---

## 🎨 Architecture

### Clean Architecture Layers

```
Presentation (API)
    ↓ depends on
Application (Business Logic)
    ↓ depends on
Domain (Core)
    ↓ implemented by
Infrastructure (Data Access)
```

### Multi-Tenant Isolation

**Standard Tenants:** Schema-based (1 DB, multiple schemas)
- `company_a.products`, `company_b.products`, etc.

**Premium Tenants:** Database-based (1 DB per tenant)
- Complete isolation, independent scaling

---

## 🔧 Configuration

**Environment Variables:**

| Variable | Description | Default |
|-----------|-------------|----------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | `Development` |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | Required |
| `Redis__ConnectionString` | Redis connection string | `localhost:6379` |

---

## 📊 Technology Stack

- **.NET 8** - Runtime framework
- **ASP.NET Core Web API** - API framework
- **Entity Framework Core** - ORM
- **SQL Server** - Database
- **Redis** - Caching
- **RabbitMQ** - Message queue (for events)
- **Docker** - Containerization
- **Kubernetes** - Orchestration
- **GitHub Actions** - CI/CD
- **xUnit** - Testing
- **NBomber/k6** - Load testing
