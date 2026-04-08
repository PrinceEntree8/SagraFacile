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
Migrations live in `SagraFacile.Infrastructure` and must reference the Web project as startup:
```bash
# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project src/SagraFacile.Infrastructure \
  --startup-project src/SagraFacile.Web/SagraFacile.Web

# Apply migrations to the database
dotnet ef database update \
  --project src/SagraFacile.Infrastructure \
  --startup-project src/SagraFacile.Web/SagraFacile.Web
```

## 2. Testing Information

### Test Project Configuration
The project uses **xUnit** for testing:
- **Application tests** (`tests/SagraFacile.Application.Tests/`) — unit tests using **NSubstitute** to mock repository interfaces.
- **Infrastructure tests** (`tests/SagraFacile.Infrastructure.Tests/`) — repository tests using **SQLite in-memory** via `TestDbContextFactory` (not EF InMemory, which doesn't support `ExecuteUpdateAsync`).

### Running Tests
```bash
dotnet test
```

### Adding New Tests

**Unit test for a handler (Application layer):**
```csharp
using NSubstitute;
using SagraFacile.Application.Features.[FeatureName];
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.[FeatureName];

public class Get[Entity]HandlerTests
{
    private readonly I[Entity]Repository _repository = Substitute.For<I[Entity]Repository>();
    private readonly Get[Entities].Handler _handler;

    public Get[Entity]HandlerTests() => _handler = new Get[Entities].Handler(_repository);

    [Fact]
    public async Task Handle_Returns[Entity]Dto()
    {
        // Arrange
        var entities = new List<[Entity]> { new() { Id = 1, Name = "Test" } };
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);

        // Act
        var result = await _handler.Handle(new Get[Entities].Query(), CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Test", result.Items[0].Name);
    }
}
```

## 3. Architecture and Code Style

### 4-Layer Clean Architecture
The project is split into four focused projects:

| Project | Responsibility |
|---|---|
| `SagraFacile.Domain` | Plain entity classes, no dependencies |
| `SagraFacile.Application` | CQRS handlers, repository interfaces, DTOs |
| `SagraFacile.Infrastructure` | EF Core, repository implementations, Identity |
| `SagraFacile.Web` | Blazor pages, SignalR hubs, controllers |

### Repository Pattern
- **Interfaces** live in `SagraFacile.Application/Interfaces/` (e.g., `IEventRepository`).
- **Implementations** live in `SagraFacile.Infrastructure/Repositories/` (e.g., `EventRepository`).
- Handlers depend **only** on the interface, never on `DbContext` or concrete classes.

### CQRS with Custom IMediator
The project uses a custom in-house `IMediator` — **not** MediatR, **not** Wolverine.

```csharp
// In Blazor pages
@inject IMediator Mediator

// Query (read)
var result = await Mediator.QueryAsync(new Get[Entities].Query());

// Command (write)
var result = await Mediator.SendAsync(new Create[Entity].Command(...));
```

Handlers implement `IQueryHandler<TQuery, TResult>` or `ICommandHandler<TCommand, TResult>`.

### Real-time Communication (SignalR)
- Hub: `Hubs/ReservationHub.cs`, mapped at `/hubs/reservations`.
- Broadcast from Application handlers by injecting `IHubContext<ReservationHub>`.

### Code Style Guidelines
- **File-scoped namespaces**: Always use `namespace MyNamespace;`.
- **Primary Constructors**: Prefer C# 12+ primary constructors for dependency injection.
- **Records**: Use `record` for DTOs, Commands, and Queries to ensure immutability.
- **Async/Await**: Always use `Async` suffix for asynchronous methods and ensure `CancellationToken` is propagated.
- **Never expose entities from queries**: Always return DTO records.

## 4. Internationalisation (i18n)

The app supports **Italian (default)** and **English** via ASP.NET Core `IStringLocalizer<SharedResource>`. The default culture is `it`.

### Key files
| File | Purpose |
|---|---|
| `Resources/SharedResource.cs` | Marker class for typed `IStringLocalizer<SharedResource>` |
| `Resources/SharedResource.resx` | English strings (neutral/fallback) |
| `Resources/SharedResource.it.resx` | Italian strings (served by default) |

### Mandatory rules for every new page/component

1. **Never hardcode user-visible strings** in `.razor` files.

2. **Add keys to both resource files** before referencing them:
   ```xml
   <!-- SharedResource.resx -->
   <data name="MyPage_Heading"><value>My Page</value></data>
   <data name="MyPage_Saved"><value>'{0}' saved!</value></data>

   <!-- SharedResource.it.resx -->
   <data name="MyPage_Heading"><value>La Mia Pagina</value></data>
   <data name="MyPage_Saved"><value>'{0}' salvato!</value></data>
   ```

3. **Inject the localiser** in the component:
   ```razor
   @inject IStringLocalizer<SharedResource> L
   ```
   (`Microsoft.Extensions.Localization` and `SagraFacile.Web.Resources` are imported globally in `_Imports.razor` — no extra `@using` needed.)

4. **Use in markup**:
   ```razor
   <h1>@L["MyPage_Heading"]</h1>
   <div class="alert alert-success">@string.Format(L["MyPage_Saved"], name)</div>
   ```

5. **Validation messages** — do **not** use `DataAnnotations [Required(ErrorMessage = "key")]` with `<DataAnnotationsValidator>`, because Blazor's `DataAnnotationsValidator` cannot resolve `IStringLocalizer` keys. Validate manually in the submit handler instead:
   ```csharp
   private async Task HandleSubmit()
   {
       errorMessage = string.Empty;
       if (string.IsNullOrWhiteSpace(model.Name))
       {
           errorMessage = L["MyPage_NameRequired"];
           return;
       }
       // proceed…
   }
   ```

6. **Key naming convention**: `[Page]_[Element]`
   Examples: `Nav_Home`, `Receptionist_Submit`, `Report_ColStatus`, `Events_Loading`.

7. **Language switching** is determined automatically by the browser's `Accept-Language` header via `AcceptLanguageHeaderRequestCultureProvider`. No extra work is needed in new pages — there is no manual language switcher in the UI.

