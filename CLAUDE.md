# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~ClassName"

# Run the app (requires PostgreSQL running)
cd src/SagraFacile.Web && dotnet run

# Add a migration
dotnet ef migrations add <MigrationName> --project src/SagraFacile.Infrastructure --startup-project src/SagraFacile.Web

# Apply migrations
dotnet ef database update --project src/SagraFacile.Infrastructure --startup-project src/SagraFacile.Web

# Docker (dev)
docker compose up -d

# Docker (production)
docker compose -f docker-compose.release.yml up -d

# Docker (LAN/offline – HTTP only)
docker compose -f docker-compose.lan.yml up -d
```

## Architecture

4-layer Clean Architecture with vertical slices:

```
Domain → Application → Infrastructure → Web
```

- **Domain** – Pure entities, no dependencies
- **Application** – CQRS handlers, repository interfaces, DTOs, FluentValidation validators
- **Infrastructure** – EF Core with PostgreSQL, concrete repositories, ASP.NET Identity
- **Web** – Blazor Web App (Server + WebAssembly Auto), SignalR hub, JWT auth REST controller

### Custom CQRS mediator (not MediatR)

All reads and writes go through an in-house `IMediator` defined in `SagraFacile.Application`. The interfaces are:
- `IQuery<TResult>` / `IQueryHandler<TQuery, TResult>`
- `ICommand<TResult>` / `ICommandHandler<TCommand, TResult>`

In Blazor pages: `@inject IMediator Mediator` → `await Mediator.Send(new Query(...), ct)`.

### Vertical slice layout

Each feature lives under `Features/<EntityName>/` in both Application and Domain layers:
```
Features/Reservations/
├── TableReservation.cs        ← domain entity
├── CreateReservation.cs       ← Command + Handler + Validator
├── GetReservations.cs         ← Query + Handler
└── ...
```

### Real-time updates via SignalR

The hub lives at `/hubs/reservations`. Blazor pages that need live data build an `HubConnection` client-side and listen for events:  
`ReservationCreated`, `ReservationCalled`, `ReservationSeated`, `ReservationVoided`, `AvailableSeatsUpdated`.  
Server-side, handlers inject `IHubContext<ReservationHub>` and call `Clients.All.SendAsync(...)`.

### Authentication

- Cookie-based auth for Blazor (8-hour sliding expiration).
- JWT tokens issued by `POST /api/auth/login` for external clients (valid 8 hours, embeds roles and `displayName` claims).
- Roles: `Admin`, `Cassiere` (cashier), `Cucina` (kitchen), `Supervisore`.
- `AllowHttp` env flag disables HTTPS redirect (used for LAN/offline deployment).

### Localization

All UI strings must go through `IStringLocalizer<SharedResource>`, injected as `@inject IStringLocalizer<SharedResource> L`. Default culture is Italian; resource files are in `src/SagraFacile.Web/Resources/`. No hardcoded strings in Blazor components.

## Key domain relationships

- **Event** – top-level aggregate (name, date, active flag)
- **TableReservation** – queue entry for an event; statuses: `Waiting → Called → Seated | Voided`. Has a `Version` property for optimistic concurrency. Each call appends a `ReservationCall` record (audit trail).
- **Table** – physical table with cover count; statuses: `Available | Occupied | Reserved`
- **MenuItem** – price stored in cents; many-to-many with `Allergen` via `MenuItemAllergen`
- **ApplicationUser** – extends `IdentityUser` with `DisplayName`

## Configuration

Required secrets/env vars (use `dotnet user-secrets` locally or env vars in Docker):

| Key | Purpose |
|-----|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key (32+ chars). Auto-generated per session if missing — breaks token validation on restart. |
| `Seed__AdminUsername` / `Seed__AdminPassword` / `Seed__AdminEmail` | Initial admin account |

See `.env.example` for Docker Compose env vars.

## Testing

- **Application.Tests** – unit tests for CQRS handlers; uses NSubstitute for mocking repository interfaces
- **Infrastructure.Tests** – repository/EF Core integration tests

Tests live under `tests/`.
