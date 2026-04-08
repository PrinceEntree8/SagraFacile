# Copilot Instructions for SagraFacile

## Project Overview

**SagraFacile** is a web application designed to manage reservations and orders at festivals and country celebrations (sagre/feste di paese). It handles table reservations, queue management, real-time calling of customers, and table cover tracking.

**Technology Stack:**
- **.NET 10 / C#** — Backend and UI framework
- **Blazor Web App** — Interactive Server render mode
- **PostgreSQL 17** — Primary database
- **Entity Framework Core 10** — ORM (Fluent API configuration)
- **Custom IMediator** — In-house CQRS mediator (`SagraFacile.Application.Infrastructure.CQRS`)
- **SignalR** — Real-time WebSocket communication
- **FluentValidation** — Request/command validation
- **ASP.NET Core Localization** — i18n with `IStringLocalizer<SharedResource>`
- **Docker / Docker Compose** — Development and production containerization

---

## Repository Structure

```
SagraFacile/
├── src/
│   ├── SagraFacile.Domain/                  # Domain entities only (no dependencies)
│   ├── SagraFacile.Application/             # CQRS handlers, interfaces, DTOs
│   │   ├── Features/
│   │   │   ├── Events/                      # GetEvents, CreateEvent, ActivateEvent, GetActiveEvent
│   │   │   └── Reservations/                # GetReservations, CreateReservation, etc.
│   │   ├── Infrastructure/CQRS/             # IMediator, ICommand, IQuery, Mediator, extensions
│   │   └── Interfaces/                      # IEventRepository, IReservationRepository, ITableRepository
│   ├── SagraFacile.Infrastructure/          # EF Core, repositories, Identity, migrations
│   │   ├── Data/ApplicationDbContext.cs
│   │   ├── Identity/ApplicationUser.cs, DataSeeder.cs
│   │   ├── Migrations/                      # EF Core migrations (run from here)
│   │   └── Repositories/                    # EventRepository, ReservationRepository, TableRepository
│   └── SagraFacile.Web/
│       └── SagraFacile.Web/                 # ASP.NET Core + Blazor Server project
│           ├── Components/
│           │   ├── Layout/NavMenu.razor      # NavMenu with language switcher & active event
│           │   └── Pages/                   # Blazor pages (Login, Admin, Events, Receptionist, …)
│           ├── Controllers/
│           │   ├── AuthController.cs
│           │   └── CultureController.cs     # GET /culture?culture=it&redirectUri=/ (i18n)
│           ├── Hubs/ReservationHub.cs
│           └── Resources/                   # i18n resource files
│               ├── SharedResource.cs        # Marker class for IStringLocalizer<SharedResource>
│               ├── SharedResource.resx      # English strings (neutral/fallback)
│               └── SharedResource.it.resx   # Italian strings (default culture)
├── tests/
│   ├── SagraFacile.Application.Tests/       # Unit tests (NSubstitute mocks)
│   └── SagraFacile.Infrastructure.Tests/   # Repository tests (SQLite in-memory)
├── docs/
│   └── DEVELOPMENT.md
├── docker-compose.yml
├── docker-compose.release.yml
└── Dockerfile
```

---

## Architecture: 4-Layer Clean Architecture

This project follows **4-layer Clean Architecture** with a **Vertical Slice** feature organisation inside the Application layer.

**Layer responsibilities:**
- **Domain** — Plain entity classes, no dependencies.
- **Application** — CQRS handlers, repository interfaces, DTOs. Depends only on Domain.
- **Infrastructure** — EF Core DbContext, repository implementations, ASP.NET Identity. Depends on Application.
- **Web** — Blazor pages, SignalR hubs, controllers. Depends on Application + Infrastructure.

**Golden rule:** Blazor pages must **never** call the database directly. All reads and writes go through `IMediator`.

**Namespace conventions:**
```csharp
// Domain
namespace SagraFacile.Domain.Features.[FeatureName];

// Application
namespace SagraFacile.Application.Features.[FeatureName];

// Infrastructure
namespace SagraFacile.Infrastructure.Repositories;

// Web
namespace SagraFacile.Web.Controllers;
```

---

## CQRS Pattern (Custom IMediator)

The project uses a custom in-house `IMediator` — not MediatR, not Wolverine.

**Interfaces:**
```csharp
// Read operation
public record Query(...) : IQuery<Result>;

// Write operation
public record Command(...) : ICommand<Result>;
```

**Handler pattern (Application layer):**
```csharp
namespace SagraFacile.Application.Features.[FeatureName];

public static class Get[Entities]
{
    public record Query(/* params */) : IQuery<Result>;
    public record Result(List<ItemDto> Items);
    public record ItemDto(int Id, string Name);   // never expose entities

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly I[Entity]Repository _repository;
        public Handler(I[Entity]Repository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var items = await _repository.GetAllAsync(cancellationToken);
            return new Result(items.Select(i => new ItemDto(i.Id, i.Name)).ToList());
        }
    }
}
```

**Dispatching from Blazor pages:**
```csharp
@inject IMediator Mediator

// Query (read)
var result = await Mediator.QueryAsync(new Get[Entities].Query(...));

// Command (write)
var result = await Mediator.SendAsync(new Create[Entity].Command(...));
```

---

## Repository Pattern

- **Interfaces** live in `SagraFacile.Application/Interfaces/` (e.g., `IEventRepository`).
- **Implementations** live in `SagraFacile.Infrastructure/Repositories/` (e.g., `EventRepository`).
- Handlers depend only on the **interface**, never on the concrete class or `DbContext`.
- Register both together in `SagraFacile.Infrastructure/DependencyInjection.cs`.

---

## Adding a New Feature

### 1. Domain entity (`SagraFacile.Domain`)
```csharp
namespace SagraFacile.Domain.Features.[FeatureName];

public class [Entity]
{
    public int Id { get; set; }
    // properties…
}
```

### 2. Repository interface (`SagraFacile.Application/Interfaces/`)
```csharp
public interface I[Entity]Repository
{
    Task<[Entity]?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<[Entity]>> GetAllAsync(CancellationToken ct);
    Task AddAsync([Entity] entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
```

### 3. CQRS handler(s) (`SagraFacile.Application/Features/[FeatureName]/`)
Follow the `Get[Entities]` / `Create[Entity]` pattern shown in **CQRS Pattern** above.

### 4. Register in `ApplicationDbContext` (`SagraFacile.Infrastructure`)
```csharp
public DbSet<[Entity]> [Entities] => Set<[Entity]>();
```

### 5. EF Core migration
```bash
dotnet ef migrations add Add[Entity] \
  --project src/SagraFacile.Infrastructure \
  --startup-project src/SagraFacile.Web/SagraFacile.Web
dotnet ef database update \
  --project src/SagraFacile.Infrastructure \
  --startup-project src/SagraFacile.Web/SagraFacile.Web
```

### 6. Add i18n strings for all new UI text — see **Internationalisation** section below.

---

## Internationalisation (i18n)

The app supports **Italian (default)** and **English**, using ASP.NET Core `IStringLocalizer<SharedResource>`.

### Key files
| File | Purpose |
|---|---|
| `Resources/SharedResource.cs` | Marker class for typed localiser |
| `Resources/SharedResource.resx` | English strings (neutral/fallback) |
| `Resources/SharedResource.it.resx` | Italian strings (default culture) |
| `Controllers/CultureController.cs` | `GET /culture?culture=it&redirectUri=/` — sets cookie |

### Rules — follow these for every new Blazor page/component

1. **Never hardcode user-visible strings** in `.razor` files.
2. **Add keys to both resource files** before using them in markup:
   ```xml
   <!-- SharedResource.resx (English) -->
   <data name="MyFeature_Heading"><value>My Feature</value></data>
   <data name="MyFeature_Created"><value>'{0}' created!</value></data>

   <!-- SharedResource.it.resx (Italian) -->
   <data name="MyFeature_Heading"><value>La Mia Funzione</value></data>
   <data name="MyFeature_Created"><value>'{0}' creato!</value></data>
   ```
3. **Inject the localiser** in every page/component that displays text:
   ```razor
   @inject IStringLocalizer<SharedResource> L
   ```
   (`Microsoft.Extensions.Localization` and `SagraFacile.Web.Resources` are already imported via `_Imports.razor`.)
4. **Use the localiser** in markup:
   ```razor
   <h1>@L["MyFeature_Heading"]</h1>

   @* Parameterised strings *@
   <div class="alert alert-success">@string.Format(L["MyFeature_Created"], name)</div>

   @* Accessing the raw string value (e.g. for aria labels) *@
   <span>@L["MyFeature_Heading"].Value</span>
   ```
5. **Key naming convention:** `[Page/Component]_[Element]`
   - `Nav_Home`, `Nav_Logout`
   - `Receptionist_CustomerName`, `Receptionist_Submit`
   - `Report_ColStatus`, `Report_Loading`
6. **Validation messages** — do **not** use `DataAnnotations [Required(ErrorMessage = "key")]` with `<DataAnnotationsValidator>`. Blazor's `DataAnnotationsValidator` cannot resolve `IStringLocalizer` keys at runtime. Validate manually in the submit handler:
   ```csharp
   if (string.IsNullOrWhiteSpace(model.Name))
   {
       errorMessage = L["MyFeature_NameRequired"];
       return;
   }
   ```
7. **Language switching** — the NavMenu already renders 🇮🇹/🇬🇧 buttons that call:
   ```csharp
   Navigation.NavigateTo(
       $"/culture?culture={lang}&redirectUri={Uri.EscapeDataString(Navigation.Uri)}",
       forceLoad: true);
   ```
   No additional wiring is needed in new pages.

---

## Real-Time Updates with SignalR

The SignalR hub is mapped at `/hubs/reservations`. Broadcast from Application handlers by injecting `IHubContext<ReservationHub>`.

**Blazor client pattern:**
```csharp
@inject NavigationManager Navigation
@implements IAsyncDisposable

private HubConnection? _hubConnection;

protected override async Task OnInitializedAsync()
{
    _hubConnection = new HubConnectionBuilder()
        .WithUrl(Navigation.ToAbsoluteUri("/hubs/reservations"))
        .WithAutomaticReconnect()
        .Build();

    _hubConnection.On<int, string>("EventName", async (id, name) =>
    {
        // update state
        await InvokeAsync(StateHasChanged);
    });

    await _hubConnection.StartAsync();
}

public async ValueTask DisposeAsync()
{
    if (_hubConnection is not null)
        await _hubConnection.DisposeAsync();
}
```

---

## Coding Conventions

- **Nullable reference types** are enabled — always handle nullability explicitly.
- **Implicit usings** are enabled — no need to add standard `using` statements manually.
- Use `async`/`await` consistently. Avoid `.Result` or `.Wait()`.
- Use **records** for commands, queries, and DTOs.
- Never expose EF Core entities directly from queries — return explicit DTO records.
- Use **FluentValidation** for all commands.
- Use **structured logging** (`ILogger<T>`) in handlers; avoid `Console.Write`.
- Use the **Result pattern** or typed exceptions for error handling.
- Entity IDs should be `int`; use auto-increment PKs.
- Naming: commands `Create[Entity]`, queries `Get[Entities]`, pages reflect their role.
- **All user-visible strings in Blazor components must use `IStringLocalizer<SharedResource>`** — see the Internationalisation section above. This is mandatory for every new page and component.

---

## Build and Run

### Local Development (PostgreSQL in Docker)

```bash
# Start PostgreSQL only
docker-compose up -d postgres

# Set connection string via user secrets
cd src/SagraFacile.Web/SagraFacile.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=postgres"

# Apply migrations and run
dotnet ef database update \
  --project ../../SagraFacile.Infrastructure \
  --startup-project .
dotnet run
```

App available at: http://localhost:5254

### Full Docker Development

```bash
docker-compose up -d
```

App available at: http://localhost:5000

### Build & Test

```bash
dotnet build
dotnet test
```

---

## Environment Variables

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key (required) |
| `Seed__AdminPassword` | Default admin password |
| `POSTGRES_USER` | PostgreSQL username (Docker) |
| `POSTGRES_PASSWORD` | PostgreSQL password (Docker) |
| `POSTGRES_DB` | PostgreSQL database name (Docker) |
| `WEB_PORT` | External web port (Docker, default 5000) |

---

## Security Notes

- Never hardcode credentials in source files.
- Use **user secrets** for local development and **environment variables** for production.
- The JWT key must be set via the `Jwt__Key` environment variable.
- The default admin password is set via the `Seed__AdminPassword` environment variable.
- Always validate inputs with FluentValidation before persisting to the database.

