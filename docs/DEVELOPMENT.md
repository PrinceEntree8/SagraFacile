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
       public record Command(...);
       public record Result(...);
       
       public class Validator : AbstractValidator<Command> { }
       
       public static async Task<Result> Handle(
           Command command, 
           ApplicationDbContext context, 
           CancellationToken cancellationToken)
       {
           // Handler logic
       }
   }
   ```

4. **Update DbContext**:
   Add the entity to `ApplicationDbContext`:
   ```csharp
   public DbSet<[Entity]> [Entities] => Set<[Entity]>();
   ```

5. **Create Migration**:
   ```bash
   dotnet ef migrations add Add[Entity] \
     --project src/SagraFacile.Infrastructure \
     --startup-project src/SagraFacile.Web/SagraFacile.Web
   ```

## Working with Wolverine

Wolverine is used for CQRS (Command Query Responsibility Segregation) and message handling:

### Commands (Write Operations)

Commands modify state:
```csharp
var command = new CreateOrder.Command("Customer Name", 100.50m);
var result = await _messageBus.InvokeAsync<CreateOrder.Result>(command);
```

### Queries (Read Operations)

Queries retrieve data:
```csharp
var query = new GetOrders.Query(Page: 1, PageSize: 10);
var result = await _messageBus.InvokeAsync<GetOrders.Result>(query);
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

## Internationalisation (i18n)

The application supports **Italian (default)** and **English** using the built-in ASP.NET Core localisation system. The default culture is `it`. The active culture is determined automatically from the browser's `Accept-Language` header on every request.

### How it works

| Component | Detail |
|---|---|
| `Resources/SharedResource.cs` | Marker class — used as type parameter for `IStringLocalizer<SharedResource>` |
| `Resources/SharedResource.resx` | English string table (neutral fallback) |
| `Resources/SharedResource.it.resx` | Italian string table (served by default) |

Registration in `Program.cs`:
```csharp
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
// …
var supportedCultures = new[] { "it", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("it"),
    SupportedCultures    = supportedCultures.Select(c => new CultureInfo(c)).ToList(),
    SupportedUICultures  = supportedCultures.Select(c => new CultureInfo(c)).ToList(),
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    }
});
```

### Adding strings for a new page

1. Open **both** resource files and add a new `<data>` element with the same key:

   ```xml
   <!-- Resources/SharedResource.resx (English) -->
   <data name="Orders_Heading" xml:space="preserve"><value>Orders</value></data>
   <data name="Orders_Created" xml:space="preserve"><value>Order '{0}' created!</value></data>

   <!-- Resources/SharedResource.it.resx (Italian) -->
   <data name="Orders_Heading" xml:space="preserve"><value>Ordini</value></data>
   <data name="Orders_Created" xml:space="preserve"><value>Ordine '{0}' creato!</value></data>
   ```

2. Inject the localiser in the Blazor page/component:

   ```razor
   @inject IStringLocalizer<SharedResource> L
   ```

   > `Microsoft.Extensions.Localization` and `SagraFacile.Web.Resources` are already imported globally via `Components/_Imports.razor`. No extra `@using` directives are needed.

3. Use the localiser in markup:

   ```razor
   <h1>@L["Orders_Heading"]</h1>

   @* Parameterised string *@
   <div class="alert alert-success">@string.Format(L["Orders_Created"], result.Name)</div>

   @* Raw string value (e.g. for HTML attributes) *@
   <input placeholder="@L["Orders_Heading"].Value" />
   ```

### Key naming convention

`[Page/Component]_[Element]` — keep it flat and descriptive:

| Key | Usage |
|---|---|
| `Nav_Home` | NavMenu – Home link |
| `Receptionist_Submit` | Receptionist page – form submit button |
| `Events_Loading` | Events page – loading message |
| `Report_ColStatus` | Report page – table column header |

### Language switching

The active language is determined by the browser's `Accept-Language` header. To change the language, users must update their browser's language preferences. New pages require **no additional wiring** — the `AcceptLanguageHeaderRequestCultureProvider` is applied automatically by the `UseRequestLocalization` middleware on every request.

---

## Best Practices

1. **Keep Slices Independent**: Avoid direct dependencies between features
2. **Use Wolverine**: All business logic should go through Wolverine handlers
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
   dotnet ef migrations list \
     --project src/SagraFacile.Infrastructure \
     --startup-project src/SagraFacile.Web/SagraFacile.Web
   ```

2. Remove problematic migration:
   ```bash
   dotnet ef migrations remove \
     --project src/SagraFacile.Infrastructure \
     --startup-project src/SagraFacile.Web/SagraFacile.Web
   ```

3. Update database:
   ```bash
   dotnet ef database update \
     --project src/SagraFacile.Infrastructure \
     --startup-project src/SagraFacile.Web/SagraFacile.Web
   ```
