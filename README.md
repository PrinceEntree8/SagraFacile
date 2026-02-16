# SagraFacile

Software per semplificare la gestione delle ordinazione alle sagre/feste di paese.

## Architecture

This project uses a **vertical-slice architecture** with the following structure:

- **Blazor Web App** with Auto interactivity mode (Server + WebAssembly)
- **PostgreSQL** database for data persistence
- **SignalR** for real-time communication
- **MediatR** for CQRS pattern implementation
- **Entity Framework Core** for database access
- **FluentValidation** for request validation

### Project Structure

```
SagraFacile/
├── src/
│   └── SagraFacile.Web/
│       ├── SagraFacile.Web/              # Server project
│       │   ├── Data/                      # Database context
│       │   ├── Features/                  # Vertical slices
│       │   │   └── Orders/                # Example feature slice
│       │   │       ├── Order.cs           # Entity
│       │   │       ├── CreateOrder.cs     # Command handler
│       │   │       └── GetOrders.cs       # Query handler
│       │   ├── Hubs/                      # SignalR hubs
│       │   └── Program.cs
│       └── SagraFacile.Web.Client/       # WebAssembly project
├── docker-compose.yml                     # Development environment
├── docker-compose.release.yml             # Production environment
└── Dockerfile
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/get-started) (for containerized setup)
- [PostgreSQL](https://www.postgresql.org/download/) (if running without Docker)

## Getting Started

### Running with Docker (Recommended)

1. Clone the repository:
   ```bash
   git clone https://github.com/PrinceEntree8/SagraFacile.git
   cd SagraFacile
   ```

2. Start the development environment:
   ```bash
   docker-compose up -d
   ```

3. The application will be available at:
   - Web App: http://localhost:5000
   - PostgreSQL: localhost:5432

4. To stop the containers:
   ```bash
   docker-compose down
   ```

### Running Locally

1. Ensure PostgreSQL is running and accessible at `localhost:5432`

2. Update connection string in `appsettings.json` if needed

3. Apply database migrations:
   ```bash
   cd src/SagraFacile.Web/SagraFacile.Web
   dotnet ef database update
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

### Production Deployment

For production deployment, use the release docker-compose file:

```bash
# Set environment variables (optional)
export POSTGRES_USER=your_user
export POSTGRES_PASSWORD=your_secure_password
export POSTGRES_DB=sagrafacile
export WEB_PORT=80

# Start production environment
docker-compose -f docker-compose.release.yml up -d
```

## Database Migrations

### Create a new migration:
```bash
cd src/SagraFacile.Web/SagraFacile.Web
dotnet ef migrations add MigrationName
```

### Apply migrations:
```bash
dotnet ef database update
```

### Remove last migration:
```bash
dotnet ef migrations remove
```

## Vertical Slice Architecture

Each feature is organized as a self-contained vertical slice with:

- **Entities**: Domain models
- **Commands**: Write operations using MediatR
- **Queries**: Read operations using MediatR
- **Validators**: FluentValidation validators for request validation
- **Handlers**: Business logic implementation

Example feature structure:
```
Features/
└── Orders/
    ├── Order.cs                # Entity
    ├── CreateOrder.cs          # Command + Handler + Validator
    └── GetOrders.cs            # Query + Handler
```

## SignalR Integration

SignalR hub is available at `/hubs/orders` for real-time order updates.

Example client usage:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/orders")
    .build();

connection.on("OrderCreated", (orderId, orderNumber) => {
    console.log(`New order created: ${orderNumber}`);
});

await connection.start();
```

## Development

### Build the solution:
```bash
dotnet build
```

### Run tests (when available):
```bash
dotnet test
```

## Technology Stack

- **Backend**: ASP.NET Core 10, Blazor Server & WebAssembly
- **Database**: PostgreSQL 17 with Entity Framework Core
- **Real-time**: SignalR
- **Patterns**: CQRS with MediatR, Vertical Slice Architecture
- **Validation**: FluentValidation
- **Containerization**: Docker & Docker Compose

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

