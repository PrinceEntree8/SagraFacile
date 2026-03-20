# Copilot Instructions for SagraFacile

## Project Overview

**SagraFacile** is a web application designed to manage reservations and orders at festivals and country celebrations (sagre/feste di paese). It handles table reservations, queue management, real-time calling of customers, and table cover tracking.

**Technology Stack:**
- **.NET 10 / C#** — Backend and UI framework
- **Blazor Web App** — Auto interactivity mode (Server + WebAssembly hybrid)
- **PostgreSQL 17** — Primary database
- **Entity Framework Core 10** — ORM (Fluent API configuration)
- **Wolverine** — CQRS (Command/Query) message bus
- **SignalR** — Real-time WebSocket communication
- **FluentValidation** — Request/command validation
- **Docker / Docker Compose** — Development and production containerization

---

## Repository Structure

```
SagraFacile/
├── src/
│   └── SagraFacile.Web/
│       ├── SagraFacile.Web/                 # Server project (ASP.NET Core)
│       │   ├── Components/                   # Blazor layout and page components
│       │   ├── Data/                         # ApplicationDbContext
│       │   ├── Features/                     # Vertical slices (one folder per feature)
│       │   │   └── [FeatureName]/
│       │   │       ├── [Entity].cs           # Domain entity
│       │   │       ├── Create[Entity].cs     # Command + Handler + Validator
│       │   │       ├── Get[Entities].cs      # Query + Handler
│       │   │       ├── Update[Entity].cs     # Command + Handler + Validator
│       │   │       └── Delete[Entity].cs     # Command + Handler
│       │   ├── Hubs/                         # SignalR hubs
│       │   ├── Infrastructure/CQRS/          # Mediator, ICommand/IQuery interfaces
│       │   ├── Migrations/                   # EF Core migration files
│       │   └── Program.cs                    # App startup and DI configuration
│       └── SagraFacile.Web.Client/          # Blazor WebAssembly client project
├── docs/
│   └── DEVELOPMENT.md                        # Detailed development guide
├── docker-compose.yml                        # Development environment
├── docker-compose.release.yml               # Production environment
└── Dockerfile
```

---

## Architecture: Vertical Slice

This project follows **Vertical Slice Architecture**. Each feature is a self-contained unit — avoid creating shared layers like "Services" or "Repositories". Cross-feature dependencies are strongly discouraged.

All business logic goes through **Wolverine handlers** (CQRS). Never call the database directly from Blazor components; always dispatch a command or query through `IMessageBus`.

**Namespace convention:**
```csharp
namespace SagraFacile.Web.Features.[FeatureName];
```

---

## Adding a New Feature

### 1. Create the folder

```bash
mkdir -p src/SagraFacile.Web/SagraFacile.Web/Features/[FeatureName]
```

### 2. Define the entity

```csharp
namespace SagraFacile.Web.Features.[FeatureName];

public class [Entity]
{
    public int Id { get; set; }
    // ... properties
}
```

### 3. Register the entity in `ApplicationDbContext`

```csharp
public DbSet<[Entity]> [Entities] => Set<[Entity]>();
```

Configure using Fluent API in `OnModelCreating`:
```csharp
modelBuilder.Entity<[Entity]>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
});
```

### 4. Create a command (write operation)

```csharp
public static class Create[Entity]
{
    public record Command(/* parameters */);
    public record Result(/* output */);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x./* property */).NotEmpty();
        }
    }

    public static async Task<Result> Handle(
        Command command,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // Business logic here
        await context.SaveChangesAsync(cancellationToken);
        return new Result(/* ... */);
    }
}
```

### 5. Create a query (read operation)

```csharp
public static class Get[Entities]
{
    public record Query(/* parameters */);
    public record Result(/* output */);

    public static async Task<Result> Handle(
        Query query,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // Query logic — return DTOs, never expose entities directly
        return new Result(/* ... */);
    }
}
```

### 6. Create a migration

```bash
cd src/SagraFacile.Web/SagraFacile.Web
dotnet ef migrations add Add[Entity]
dotnet ef database update
```

---

## CQRS Pattern with Wolverine

Dispatch commands and queries from Blazor components using the injected `IMessageBus`:

```csharp
@inject IMessageBus MessageBus

// Command
var result = await MessageBus.InvokeAsync<Create[Entity].Result>(new Create[Entity].Command(...));

// Query
var result = await MessageBus.InvokeAsync<Get[Entities].Result>(new Get[Entities].Query(...));
```

---

## Real-Time Updates with SignalR

The SignalR hub is mapped at `/hubs/reservations`. When a command modifies state that other clients should observe, broadcast from the handler by injecting `IHubContext<ReservationHub>`.

**Blazor client pattern:**

```csharp
@inject NavigationManager Navigation
@implements IAsyncDisposable

private HubConnection? _hubConnection;

protected override async Task OnInitializedAsync()
{
    _hubConnection = new HubConnectionBuilder()
        .WithUrl(Navigation.ToAbsoluteUri("/hubs/reservations"))
        .Build();

    _hubConnection.On</* args */>("EventName", (/* args */) =>
    {
        // Update state
        StateHasChanged();
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
- Use the **Result pattern** or typed exceptions for error handling — avoid unhandled exceptions propagating to the UI.
- Entity IDs should be `int`; use auto-increment PKs.
- Naming: commands `Create[Entity]`, queries `Get[Entities]`, pages `[Role].razor`.

---

## Build and Run

### Local Development (with Docker for PostgreSQL)

```bash
# Start PostgreSQL only
docker-compose up -d postgres

# Set connection string via user secrets
cd src/SagraFacile.Web/SagraFacile.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=postgres"

# Apply migrations and run
dotnet ef database update
dotnet run
```

App available at: http://localhost:5254

### Full Docker Development

```bash
docker-compose up -d
```

App available at: http://localhost:5000

### Build

```bash
dotnet build
```

### Tests

```bash
dotnet test
```

> Note: Test infrastructure uses xUnit with `WebApplicationFactory<Program>` for integration tests and in-memory DbContext for unit tests. Follow Arrange-Act-Assert pattern.

---

## Environment Variables

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `POSTGRES_USER` | PostgreSQL username (Docker) |
| `POSTGRES_PASSWORD` | PostgreSQL password (Docker) |
| `POSTGRES_DB` | PostgreSQL database name (Docker) |
| `WEB_PORT` | External web port (Docker, default 5000) |

See `.env.example` for a full list of variables used in Docker Compose.

---

## Security Notes

- Never hardcode credentials in source files.
- Use **user secrets** for local development and **environment variables** for production.
- The JWT key must be set via the `Jwt__Key` environment variable.
- The default admin password is set via the `Seed__AdminPassword` environment variable.
- Always validate inputs with FluentValidation before persisting to the database.
