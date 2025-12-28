# EMR API - Electronic Medical Records System

A .NET 10 Web API project implementing Clean Architecture for a comprehensive Electronic Medical Records (EMR) system.

## Project Structure

```
emr-api/
├── EMR.sln
├── src/
│   ├── EMR.Api/                    # Web API layer
│   ├── EMR.Application/            # Application/Use Cases layer
│   ├── EMR.Domain/                 # Domain entities, interfaces
│   └── EMR.Infrastructure/         # Data access, external services
└── tests/
    ├── EMR.UnitTests/
    └── EMR.IntegrationTests/
```

## Architecture Layers

### EMR.Domain
Core business domain layer containing:
- **Base Entity**: Audit fields and soft delete support
- **Value Objects**: UserId, PatientId
- **Domain Interfaces**: IRepository, IUnitOfWork
- **Domain Exceptions**: EntityNotFoundException, ValidationException, BusinessRuleViolationException

### EMR.Application
Application logic and use cases layer:
- **CQRS Pattern**: ICommand, IQuery, ICommandHandler, IQueryHandler interfaces
- **MediatR**: Request/response pipeline with validation and logging behaviors
- **FluentValidation**: Input validation
- **AutoMapper**: Object mapping
- **Common DTOs**: PagedResultDto, ResultDto

### EMR.Infrastructure
Data access and external services:
- **PostgreSQL**: Using Npgsql.EntityFrameworkCore.PostgreSQL
- **ApplicationDbContext**: EF Core DbContext with audit tracking
- **Repository Pattern**: Generic repository implementation
- **Unit of Work**: Transaction management
- **Redis Caching**: Distributed caching support
- **Azure Key Vault**: Configuration secrets management

### EMR.Api
RESTful API presentation layer:
- **JWT Bearer Authentication**: Azure AD B2C ready
- **Swagger/OpenAPI**: API documentation
- **CORS**: Cross-origin resource sharing
- **Health Checks**: PostgreSQL and Redis monitoring
- **Serilog**: Structured logging

## Technology Stack

- **.NET 10**
- **ASP.NET Core Web API**
- **Entity Framework Core 10**
- **PostgreSQL** (via Npgsql)
- **Redis** (StackExchange.Redis)
- **MediatR** (CQRS pattern)
- **FluentValidation**
- **AutoMapper**
- **Swashbuckle** (Swagger/OpenAPI)
- **Serilog** (Logging)
- **Azure AD B2C** (Authentication)
- **Azure Key Vault** (Secrets management)
- **xUnit** (Testing)
- **Moq** (Mocking)
- **FluentAssertions** (Test assertions)

## Getting Started

### Prerequisites
- .NET 10 SDK
- PostgreSQL database
- Redis (optional, for caching)
- Azure AD B2C tenant (for authentication)
- Azure Key Vault (optional, for secrets)

### Configuration

Update `appsettings.json` in the EMR.Api project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=emr_db;Username=postgres;Password=your_password",
    "Redis": "localhost:6379"
  },
  "AzureAdB2C": {
    "Instance": "https://your-tenant.b2clogin.com",
    "Domain": "your-tenant.onmicrosoft.com",
    "ClientId": "your-client-id",
    "TenantId": "your-tenant-id",
    "SignUpSignInPolicyId": "B2C_1_SignUpSignIn"
  }
}
```

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
cd src/EMR.Api
dotnet run

# Run tests
dotnet test
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001` (development only)

## API Endpoints

### Health Checks
- `GET /health` - Basic health check
- `GET /api/health` - Health status with application info
- `GET /api/health/detailed` - Detailed health information

## Features Implemented

### Security (Feature #52: User Authentication)
- JWT Bearer token authentication
- Azure AD B2C integration ready
- Role-based authorization policies (Admin, Doctor, Nurse)
- CORS configuration
- Health check endpoints

### Data Access
- Repository pattern with generic base implementation
- Unit of Work pattern for transaction management
- Soft delete support
- Audit fields (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
- Optimistic concurrency with row versioning
- Global query filters for soft-deleted entities

### Caching
- Distributed Redis caching
- Cache service abstraction (ICacheService)

### Logging
- Structured logging with Serilog
- Console and file sinks
- Request/response logging middleware

### Validation
- FluentValidation for input validation
- Automatic validation pipeline behavior
- Detailed validation error responses

## Development Guidelines

### Clean Architecture Principles
1. Dependencies flow inward (Presentation → Application → Domain)
2. Domain layer has no external dependencies
3. Application layer defines interfaces, Infrastructure implements them
4. Use dependency injection for all cross-layer dependencies

### CQRS Pattern
- Commands: Modify state (Create, Update, Delete)
- Queries: Read data without side effects
- Handlers: Implement business logic for commands/queries

## License

Copyright (c) 2025 EMR System. All rights reserved.
