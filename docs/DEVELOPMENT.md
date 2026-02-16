# Development Guide

## Vertical Slice Architecture

This project follows the **Vertical Slice Architecture** pattern, where each feature is organized as a self-contained vertical slice. This approach offers several benefits:

- **Feature Cohesion**: All code related to a feature lives together
- **Independent Development**: Teams can work on different slices without conflicts
- **Easier Testing**: Each slice can be tested independently
- **Reduced Coupling**: Minimal dependencies between features

### Feature Structure

Each feature slice typically includes:

```
Features/
└── [FeatureName]/
    ├── [Entity].cs              # Domain entity
    ├── Create[Entity].cs        # Command + Handler + Validator
    ├── Get[Entities].cs         # Query + Handler
    ├── Update[Entity].cs        # Command + Handler + Validator
    └── Delete[Entity].cs        # Command + Handler
```

### Creating a New Feature

1. **Create Feature Folder**:
   ```bash
   mkdir -p src/SagraFacile.Web/SagraFacile.Web/Features/[FeatureName]
   ```

2. **Add Entity**:
   ```csharp
   namespace SagraFacile.Web.Features.[FeatureName];
   
   public class [Entity]
   {
       public int Id { get; set; }
       // Add properties
   }
   ```

3. **Add Commands/Queries**:
   ```csharp
   public static class Create[Entity]
   {
       public record Command(...) : IRequest<Result>;
       public record Result(...);
       
       public class Validator : AbstractValidator<Command> { }
       public class Handler : IRequestHandler<Command, Result> { }
   }
   ```

4. **Update DbContext**:
   Add the entity to `ApplicationDbContext`:
   ```csharp
   public DbSet<[Entity]> [Entities] => Set<[Entity]>();
   ```

5. **Create Migration**:
   ```bash
   dotnet ef migrations add Add[Entity]
   ```

## Working with MediatR

MediatR implements the mediator pattern and is used for CQRS (Command Query Responsibility Segregation):

### Commands (Write Operations)

Commands modify state:
```csharp
var command = new CreateOrder.Command("Customer Name", 100.50m);
var result = await _mediator.Send(command);
```

### Queries (Read Operations)

Queries retrieve data:
```csharp
var query = new GetOrders.Query(Page: 1, PageSize: 10);
var result = await _mediator.Send(query);
```

## Working with SignalR

### Server-Side (Hub)

```csharp
public class OrderHub : Hub
{
    public async Task SendUpdate(string message)
    {
        await Clients.All.SendAsync("ReceiveUpdate", message);
    }
}
```

### Client-Side (JavaScript)

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/orders")
    .build();

connection.on("ReceiveUpdate", (message) => {
    console.log(message);
});

await connection.start();
await connection.invoke("SendUpdate", "Hello from client!");
```

### Client-Side (Blazor)

```csharp
@inject NavigationManager Navigation
@implements IAsyncDisposable

private HubConnection? hubConnection;

protected override async Task OnInitializedAsync()
{
    hubConnection = new HubConnectionBuilder()
        .WithUrl(Navigation.ToAbsoluteUri("/hubs/orders"))
        .Build();

    hubConnection.On<string>("ReceiveUpdate", (message) =>
    {
        // Handle message
        StateHasChanged();
    });

    await hubConnection.StartAsync();
}

public async ValueTask DisposeAsync()
{
    if (hubConnection is not null)
    {
        await hubConnection.DisposeAsync();
    }
}
```

## Database Operations

### Connection String

Configure in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=postgres"
  }
}
```

**Security Note**: For production and even local development, it's recommended to use environment variables or user secrets instead of hardcoding credentials:

#### Using Environment Variables
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=your_password"
```

#### Using User Secrets (Recommended for Local Development)
```bash
cd src/SagraFacile.Web/SagraFacile.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=your_password"
```

The application will use connection strings in this priority order:
1. Environment variable `ConnectionStrings__DefaultConnection`
2. User secrets (in development)
3. `appsettings.{Environment}.json`
4. `appsettings.json`
5. Fallback default in `Program.cs`

### Entity Configuration

Use Fluent API in `OnModelCreating`:
```csharp
modelBuilder.Entity<Order>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
    entity.HasIndex(e => e.OrderNumber).IsUnique();
});
```

## Docker Development

### Start Development Environment
```bash
docker-compose up -d
```

### View Logs
```bash
docker-compose logs -f web
docker-compose logs -f postgres
```

### Rebuild After Code Changes
```bash
docker-compose up -d --build
```

### Access PostgreSQL
```bash
docker exec -it sagrafacile-postgres-dev psql -U postgres -d sagrafacile
```

### Common Commands
```bash
# Stop containers
docker-compose down

# Stop and remove volumes (deletes database)
docker-compose down -v

# View running containers
docker-compose ps
```

## Testing

### Unit Tests

Test individual handlers:
```csharp
[Fact]
public async Task CreateOrder_Should_Generate_Unique_OrderNumber()
{
    // Arrange
    var context = CreateInMemoryDbContext();
    var handler = new CreateOrder.Handler(context);
    var command = new CreateOrder.Command("Test Customer", 100m);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.NotNull(result.OrderNumber);
}
```

### Integration Tests

Test complete slices with database:
```csharp
public class OrderFeatureTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Tests here
}
```

## Best Practices

1. **Keep Slices Independent**: Avoid direct dependencies between features
2. **Use MediatR**: All business logic should go through MediatR handlers
3. **Validate Early**: Use FluentValidation for all commands
4. **Async All The Way**: Use async/await consistently
5. **Explicit DTOs**: Don't expose entities directly; use DTOs in queries
6. **Transaction Boundaries**: Let handlers define transaction boundaries
7. **Error Handling**: Use Result pattern or custom exceptions
8. **Logging**: Add structured logging to handlers

## Troubleshooting

### Cannot Connect to Database

1. Check if PostgreSQL is running:
   ```bash
   docker-compose ps postgres
   ```

2. Verify connection string in `appsettings.json`

3. Check PostgreSQL logs:
   ```bash
   docker-compose logs postgres
   ```

### Build Errors

1. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

2. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

### Migration Issues

1. Check migration list:
   ```bash
   dotnet ef migrations list
   ```

2. Remove problematic migration:
   ```bash
   dotnet ef migrations remove
   ```

3. Update database:
   ```bash
   dotnet ef database update
   ```
