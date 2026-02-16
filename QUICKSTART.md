# Quick Start Guide

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or
- [Docker](https://www.docker.com/get-started) (Docker-only option)

## Option 1: Quick Start with Docker (Recommended)

The fastest way to get started is using Docker Compose:

```bash
# Clone the repository
git clone https://github.com/PrinceEntree8/SagraFacile.git
cd SagraFacile

# Start the entire stack (database + web app)
docker compose up -d

# Wait a few seconds for the containers to start
# Access the application at http://localhost:5000
```

To stop:
```bash
docker compose down
```

## Option 2: Local Development

If you prefer to run the application locally:

### 1. Start PostgreSQL with Docker

```bash
docker compose up -d postgres
```

### 2. Apply Database Migrations

```bash
cd src/SagraFacile.Web/SagraFacile.Web
dotnet ef database update
```

### 3. Run the Application

```bash
dotnet run
```

The application will be available at https://localhost:5254 (or http://localhost:5254)

### Configuration Notes

- **Local Development**: Uses `appsettings.Development.json` with `Host=localhost`
- **Docker Environment**: Uses `Host=postgres` (Docker service name)
- The application auto-detects the environment and uses the appropriate connection string

## Project Structure

```
SagraFacile/
├── src/
│   └── SagraFacile.Web/
│       ├── SagraFacile.Web/              # Server project (ASP.NET Core)
│       │   ├── Data/                      # Database context
│       │   ├── Features/                  # Vertical slices
│       │   │   └── Orders/                # Example: Orders feature
│       │   ├── Hubs/                      # SignalR hubs
│       │   └── Components/                # Blazor components
│       └── SagraFacile.Web.Client/       # WebAssembly project
├── docs/                                  # Documentation
├── docker-compose.yml                     # Development environment
├── docker-compose.release.yml             # Production environment
└── Dockerfile
```

## Key Features

✅ **Blazor Web App** - Auto interactivity (Server + WebAssembly)  
✅ **Vertical Slice Architecture** - Feature-focused organization  
✅ **PostgreSQL** - Robust relational database  
✅ **Entity Framework Core** - Modern ORM  
✅ **MediatR** - CQRS pattern implementation  
✅ **SignalR** - Real-time communication  
✅ **Docker** - Containerized deployment  
✅ **FluentValidation** - Request validation  

## What's Included

### Example Feature: Orders

The project includes a complete Orders feature demonstrating the vertical slice architecture:

- **Order Entity** - Domain model with EF Core configuration
- **CreateOrder Command** - Create new orders with validation
- **GetOrders Query** - Retrieve paginated orders
- **Orders Page** - Blazor UI demonstrating CRUD operations
- **OrderHub** - SignalR hub for real-time updates

### Database

- PostgreSQL 17 Alpine
- Automatic migrations
- Configured with Entity Framework Core
- Health checks in Docker setup

### Development Tools

- Hot reload enabled
- Structured logging
- Docker development environment
- Production-ready Docker Compose configuration

## Next Steps

1. **Explore the Code**: Check out `src/SagraFacile.Web/SagraFacile.Web/Features/Orders/` for the example feature
2. **Read the Docs**: See `docs/DEVELOPMENT.md` for detailed development guide
3. **Add Your Features**: Follow the vertical slice pattern to add new features
4. **Customize**: Modify the Orders feature or create new ones

## Getting Help

- **Development Guide**: See `docs/DEVELOPMENT.md`
- **Issues**: Open an issue on GitHub
- **README**: Full README in `README.md`

## Production Deployment

For production, use the release compose file:

```bash
# Set environment variables (optional)
export POSTGRES_PASSWORD=your_secure_password
export WEB_PORT=80

# Start production stack
docker compose -f docker-compose.release.yml up -d
```

See `README.md` for complete production deployment instructions.
