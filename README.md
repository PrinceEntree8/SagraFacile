# SagraFacile

Software per semplificare la gestione delle ordinazione alle sagre/feste di paese.

## Architecture

This project uses a **vertical-slice architecture** with the following structure:

- **Blazor Web App** with Auto interactivity mode (Server + WebAssembly)
- **PostgreSQL** database for data persistence
- **SignalR** for real-time communication
- **Wolverine** for CQRS pattern implementation
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

2. **Configure database credentials** (recommended for security):
   
   Using user secrets (recommended):
   ```bash
   cd src/SagraFacile.Web/SagraFacile.Web
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=your_password"
   ```
   
   Or update connection string in `appsettings.Development.json` (not recommended for production)

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

### LAN / Offline Deployment

SagraFacile is designed to run entirely on a local network (LAN) without internet access.
Authentication and role-based access control are handled locally against the embedded
PostgreSQL database — no external identity provider is needed.

Use `docker-compose.lan.yml` for a pre-configured, HTTP-only LAN deployment:

```bash
# (Optional) Override defaults via environment variables or a .env file
# cp .env.example .env   # then edit passwords and keys

# Build and start the LAN stack
docker compose -f docker-compose.lan.yml up -d
```

The app will be available at **`http://<server-ip>:80`** from any device on the same network.

#### Accessing from other devices

Find the server's LAN IP address:
```bash
# Linux / macOS
ip route get 1 | awk '{print $NF; exit}'   # or: hostname -I

# Windows (PowerShell)
(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notmatch 'Loopback' }).IPAddress
```

Then open `http://<server-ip>` in any browser on the same network.

#### Default credentials (change before sharing with others!)

| Setting | Default |
|---|---|
| Admin username | `admin` |
| Admin password | `Admin@123!` |
| Database password | `SagraLAN2026!` |
| JWT signing key | `SagraFacile-LAN-Change-This-Key-Before-Sharing!` |

Override defaults by creating a `.env` file (see `.env.example`) or by passing environment variables:

```bash
Jwt__Key=my-long-random-secret \
Seed__AdminPassword=MyStr0ngP@ss! \
docker compose -f docker-compose.lan.yml up -d
```

#### Offline behaviour notes

- **Authentication** works entirely offline — no internet calls are made during login or token validation.
- **Roles** (`Admin`, `Cassiere`, `Cucina`, `Supervisore`) are stored in the local database and evaluated locally.
- If `Jwt:Key` is not set, the app auto-generates a random key at startup. Sessions are invalidated on every restart. Configure a fixed key for stable sessions across restarts.
- Set `AllowHttp=true` (already set in `docker-compose.lan.yml`) to disable HTTPS redirect and HSTS when running over plain HTTP.

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
- **Commands**: Write operations using Wolverine
- **Queries**: Read operations using Wolverine
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
- **Patterns**: CQRS with Wolverine, Vertical Slice Architecture
- **Validation**: FluentValidation
- **Containerization**: Docker & Docker Compose

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

