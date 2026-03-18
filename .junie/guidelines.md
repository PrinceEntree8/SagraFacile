# SagraFacile Development Guidelines

This document provides essential information for advanced developers working on the SagraFacile project.

## 1. Build and Configuration

### Prerequisites
- **.NET 10 SDK**
- **Docker** (for containerized setup)
- **PostgreSQL** (if running locally without Docker)

### Local Configuration
For local development, use **User Secrets** to manage sensitive information like connection strings:
```bash
cd src/SagraFacile.Web/SagraFacile.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=your_password"
```

### Database Migrations
Migrations are managed from the server project directory:
```bash
cd src/SagraFacile.Web/SagraFacile.Web
# Create a new migration
dotnet ef migrations add <MigrationName>
# Apply migrations to database
dotnet ef database update
```

## 2. Testing Information

### Test Project Configuration
The project uses **xUnit** for testing. To add a new test project:
1. Create a project targeting `net10.0`.
2. Add dependencies: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`.
3. Reference the relevant source projects (e.g., `SagraFacile.Web.csproj`).

### Running Tests
Execute tests using the .NET CLI:
```bash
dotnet test
```

### Adding New Tests (Example)
When adding tests for vertical slices, it is common to test the **Validators** and **Handlers** independently.

**Example Test for a Validator:**
```csharp
using FluentValidation;
using Xunit;

public class MyFeatureValidatorTests
{
    private readonly CreateMyFeature.Validator _validator = new();

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var command = new CreateMyFeature.Command(Name: "");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }
}
```

## 3. Architecture and Code Style

### Vertical Slice Architecture
The project is organized into self-contained slices under `src/SagraFacile.Web/SagraFacile.Web/Features/`. Each folder should ideally contain:
- `Entity.cs`: Domain models.
- `Create[Feature].cs`: Command, Handler, and Validator.
- `Get[Feature].cs`: Query and Handler.

### CQRS with Wolverine
- Use `ICommand<T>` for write operations and `IQuery<T>` for read operations.
- Handlers should be implemented within the same file as the command/query for better cohesion in the vertical slice.

### Real-time Communication (SignalR)
- Hubs are located in `Hubs/`.
- Use the `HubContext` within Wolverine handlers to broadcast updates after successful state changes.

### Code Style Guidelines
- **File-scoped namespaces**: Always use `namespace MyNamespace;`.
- **Primary Constructors**: Prefer C# 12+ primary constructors for dependency injection.
- **Records**: Use `record` for DTOs, Commands, and Queries to ensure immutability.
- **Async/Await**: Always use `Async` suffix for asynchronous methods and ensure `CancellationToken` is propagated.
