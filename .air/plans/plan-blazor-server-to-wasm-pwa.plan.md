# Migration Plan: Blazor Server → Blazor WASM PWA

## Context

`SagraFacile.Web` is currently a **Blazor Server** application: all Blazor components render on the
server over a persistent SignalR circuit. The goal is to split it into:

- **`SagraFacile.Web`** (backend only) – ASP.NET Core host that serves the built WASM bundle as
  static files, exposes REST API controllers, and runs the SignalR hub.
- **`SagraFacile.WebClient`** (frontend) – Blazor WebAssembly PWA containing all Blazor pages and
  components; calls the backend exclusively over HTTP and SignalR.
- **`SagraFacile.Contracts`** (shared library) – Plain-C# DTOs and request/response models shared
  between backend API responses and client deserialization; no framework deps, safe to ship to
  browser.

---

## Architecture: Before vs After

```
BEFORE
──────
Browser ──── WebSocket (Blazor circuit) ────► SagraFacile.Web
                                               ├─ Blazor components (server)
                                               ├─ IMediator (direct calls)
                                               ├─ REST controllers (/api/*)
                                               └─ SignalR hub (/hubs/*)

AFTER
─────
Browser ──── HTTP/HTTPS (fetch) ────────────► SagraFacile.Web  (backend)
        ──── WebSocket (SignalR hub only) ──►  ├─ REST controllers (/api/*)
        ◄─── Static files (WASM bundle) ─────  ├─ SignalR hub (/hubs/*)
                                               └─ wwwroot/ → WebClient publish output
        SagraFacile.WebClient  (in browser)
        ├─ Blazor WASM PWA
        ├─ typed HttpClient services
        ├─ JWT AuthStateProvider
        └─ SignalR HubConnection (JWT via query param)
```

---

## New Project: `SagraFacile.Contracts`

**Path:** `src/SagraFacile.Contracts/`

**Purpose:** Shared request/response models for the REST API. Both the Web controllers (return
these types) and the WebClient typed services (deserialize into these types) reference this library.
It intentionally has **zero framework dependencies** so it compiles for both the server runtime and
the WebAssembly target.

### Files to create

| File | Contents |
|------|---------|
| `SagraFacile.Contracts.csproj` | `net10.0`, no dependencies |
| `Auth/LoginRequest.cs` | `record LoginRequest(string Username, string Password)` |
| `Auth/LoginResponse.cs` | `record LoginResponse(string Token, string Username, string DisplayName, IList<string> Roles, DateTimeOffset ExpiresAt)` |
| `Events/EventDto.cs` | Maps to `SagraFacile.Application` `EventDto`; fields: Id, Name, Date, IsActive |
| `Events/CreateEventRequest.cs` | Name, Date |
| `Events/EventAdditionalOptionsDto.cs` | IsPartyCompletionEnabled, MinPartySize |
| `Events/UpdateEventAdditionalOptionsRequest.cs` | IsPartyCompletionEnabled, MinPartySize |
| `Menu/MenuCategoryDto.cs` | Id, Name, SortOrder, EventId, list of MenuItemDto |
| `Menu/MenuItemDto.cs` | Id, Name, Description, PriceCents, list of AllergenDto |
| `Menu/AllergenDto.cs` | Id, Name |
| `Menu/MenuDetailsDto.cs` | WelcomeMessage, FooterMessage |
| `Menu/CreateMenuCategoryRequest.cs` | EventId, Name, SortOrder |
| `Menu/UpdateMenuCategoryRequest.cs` | Name, SortOrder |
| `Menu/CreateMenuItemRequest.cs` | CategoryId, Name, Description, PriceCents, AllergenIds |
| `Menu/UpdateMenuItemRequest.cs` | Name, Description, PriceCents, AllergenIds |
| `Menu/UpdateMenuDetailsRequest.cs` | WelcomeMessage, FooterMessage |
| `Reservations/ReservationDto.cs` | Mirror of Application `ReservationDto`; all fields |
| `Reservations/ReservationCounterDto.cs` | Status, Count |
| `Reservations/ReservationReportDto.cs` | Matches Application report DTO |
| `Reservations/CreateReservationRequest.cs` | (already in Web controllers; move here) |
| `Reservations/CallReservationRequest.cs` | (already in Web controllers; move here) |
| `Reservations/CallAndSeatRequest.cs` | (already in Web controllers; move here) |
| `Reservations/EditReservationRequest.cs` | CustomerName, PartySize, Notes |
| `Users/UserDto.cs` | Id, UserName, DisplayName, Email, list of string Roles |
| `Users/CreateUserRequest.cs` | UserName, DisplayName, Email, Password, Roles |
| `Users/AssignRolesRequest.cs` | list of string Roles |

> **Trade-off note:** The Application layer already defines these DTOs. Duplicating them in
> Contracts adds a minor maintenance surface but avoids shipping FluentValidation and CQRS
> abstractions to the browser. Junie should mirror the Application DTO shapes exactly so JSON
> serialization is compatible without custom converters.

---

## New Project: `SagraFacile.WebClient`

**Path:** `src/SagraFacile.WebClient/`

**Template:** Blazor WebAssembly Standalone App (PWA) — `dotnet new blazorwasm --pwa`

**Target framework:** `net10.0`

### .csproj key configuration

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ServiceWorkerInclude>true</ServiceWorkerInclude>
    <ServiceWorkerContent>wwwroot/service-worker.js</ServiceWorkerContent>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.*" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.*" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.*" />
    <PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SagraFacile.Contracts\SagraFacile.Contracts.csproj" />
  </ItemGroup>
</Project>
```

### Files to create / migrate

#### Authentication

| File | Description |
|------|-------------|
| `Auth/JwtAuthStateProvider.cs` | Implements `AuthenticationStateProvider`; reads JWT from `localStorage`, parses claims with `System.IdentityModel.Tokens.Jwt`, exposes user state. |
| `Auth/AuthService.cs` | `LoginAsync(LoginRequest)` → calls `POST /api/auth/login`, stores token in `localStorage`, notifies `JwtAuthStateProvider`. `LogoutAsync()` → clears localStorage, notifies provider, redirects. |
| `Auth/TokenStorageService.cs` | Thin wrapper over `IJSRuntime` to get/set/remove `"auth_token"` key in localStorage. |

> **JWT parsing in WASM:** Use `System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler` (works in
> WASM) to extract claims (roles, displayName, sub) without calling back to server.

#### Typed HTTP Client Services

Create one service per resource group. Each is registered as a scoped typed `HttpClient` client in
`Program.cs`. All clients use **relative base addresses** (`/api/` and `/hubs/`) because in both
development and production the WebClient is always served by the same origin as the Web backend.
The `HttpClient.BaseAddress` is set to the browser's current origin (`builder.HostEnvironment.BaseAddress`)
and service methods use relative paths like `api/events`. Tokens are attached via a shared
`DelegatingHandler`.

| Service class | Interface | Methods |
|--------------|-----------|---------|
| `Services/AuthHttpService.cs` | `IAuthService` | `LoginAsync`, `LogoutAsync` |
| `Services/EventsService.cs` | `IEventsService` | `GetEventsAsync`, `CreateEventAsync`, `ActivateEventAsync`, `GetActiveEventAsync`, `GetEventOptionsAsync`, `UpdateEventOptionsAsync` |
| `Services/MenuService.cs` | `IMenuService` | `GetMenuAsync`, `GetCategoriesAsync`, `CreateCategoryAsync`, `UpdateCategoryAsync`, `DeleteCategoryAsync`, `CreateItemAsync`, `UpdateItemAsync`, `DeleteItemAsync`, `GetAllergensAsync`, `GetMenuDetailsAsync`, `UpdateMenuDetailsAsync` |
| `Services/ReservationsService.cs` | `IReservationsService` | `GetReservationsAsync(filter)`, `CreateAsync`, `CallAsync`, `SeatAsync`, `CallAndSeatAsync`, `VoidAsync`, `EditAsync`, `MarkPartyCompleteAsync`, `GetCountersAsync`, `GetBestFitAsync`, `GetReportAsync` |
| `Services/UsersService.cs` | `IUsersService` | `GetUsersAsync`, `CreateUserAsync`, `GetRolesAsync`, `AssignRolesAsync` |

A shared `AuthorizationMessageHandler : DelegatingHandler` reads the token from
`TokenStorageService` and adds `Authorization: Bearer` to outgoing requests.

> **No `ApiBaseUrl` configuration needed.** Since the WASM app is always served from the same
> origin as the backend, `builder.HostEnvironment.BaseAddress` is the correct base. Register the
> default `HttpClient` as:
> ```csharp
> builder.Services.AddScoped(sp =>
>     new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
> ```
> Typed service clients inherit this base address.

#### Localization

| File | Description |
|------|-------------|
| `Resources/SharedResource.cs` | Marker class (same as current) |
| `Resources/SharedResource.resx` | Copy from `SagraFacile.Web/Resources/SharedResource.resx` |
| `Resources/SharedResource.it.resx` | Copy from `SagraFacile.Web/Resources/SharedResource.it.resx` |

Register in `Program.cs`:
```csharp
builder.Services.AddLocalization();
// Detect culture from localStorage (key: "blazor-culture") or Accept-Language.
// Use CultureInfo.DefaultThreadCurrentCulture / DefaultThreadCurrentUICulture.
```

> Blazor WASM localization does not use `UseRequestLocalization` (that's server middleware).
> Culture must be set at startup by reading a stored preference or browser language.
> Use the standard Blazor WASM i18n pattern:
> `Program.cs` → read `localStorage["blazor-culture"]` via JS interop → set
> `CultureInfo.DefaultThreadCurrentCulture` before `await builder.Build().RunAsync()`.

#### Components (migrated from SagraFacile.Web)

Copy **all** files from `src/SagraFacile.Web/Components/` into `src/SagraFacile.WebClient/Components/`, then make the following changes to each:

**Global changes (apply to all components):**

1. Remove `@rendermode InteractiveServer` directives — WASM is always client-side.
2. Replace `@inject IMediator Mediator` → inject the relevant typed service (e.g., `@inject IReservationsService ReservationsService`).
3. Replace all `await Mediator.Send(new SomeQuery(...))` → `await RelevantService.GetSomethingAsync(...)`.
4. Replace `@inject UserManager<ApplicationUser> UserManager` and `SignInManager` → `@inject IUsersService UsersService` / `@inject AuthService AuthService`.
5. Replace `NavigationManager.NavigateTo("/logout")` POST → call `AuthService.LogoutAsync()`.

**Page-specific changes:**

| Page | Change summary |
|------|---------------|
| `Login.razor` | Remove `UserManager`/`SignInManager` injection; call `AuthService.LoginAsync()`; on success store token and navigate to `/`. |
| `Admin.razor` | Replace `UserManager` calls with `UsersService.GetUsersAsync()`, `CreateUserAsync()`, `AssignRolesAsync()`. |
| `Events.razor` | Replace `Mediator.Send(GetEvents)` → `EventsService.GetEventsAsync()`; same for create/activate. |
| `EventAdditionalOptions.razor` | Use `EventsService.GetEventOptionsAsync()` / `UpdateEventOptionsAsync()`. |
| `MenuManagement.razor` | Use `MenuService` methods. |
| `Receptionist.razor` | Use `ReservationsService` + `EventsService.GetActiveEventAsync()`; SignalR setup unchanged (see below). |
| `HeadWaiter.razor` | Use `ReservationsService`. |
| `ReservationOverview.razor` | Use `ReservationsService`. |
| `ReservationReport.razor` | Use `ReservationsService.GetReportAsync()`. |
| `Home.razor` | Use `EventsService.GetActiveEventAsync()`; SignalR unchanged. |
| `NowCalling.razor` | No data service calls; SignalR only — minimal changes. |
| `PublicMenu.razor` | Use `MenuService.GetMenuAsync(activeEventId)`; SignalR unchanged. |

**SignalR connections in components:** The existing `HubConnectionBuilder` pattern is identical
between Blazor Server and WASM. The only required change is how the token is obtained. The hub URL
is always relative (`/hubs/reservations`) since the WASM app is same-origin with the backend:

```csharp
// BEFORE (server-side — token from URL param / cookie)
.WithUrl(Navigation.ToAbsoluteUri("/hubs/reservations"))

// AFTER (WASM — token from localStorage, relative path same-origin)
var token = await TokenStorage.GetTokenAsync();
.WithUrl(Navigation.ToAbsoluteUri("/hubs/reservations"), options =>
{
    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
})
```

`AccessTokenProvider` causes the SignalR client to append `access_token=<token>` as query param
(which the server already handles in `JwtBearerEvents.OnMessageReceived`). No CORS configuration
is required since hub and client share the same origin.

#### Extensions (migrated)

Copy `src/SagraFacile.Web/Extensions/ReservationStatusExtensions.cs` and
`TimeStampFormatExtensions.cs` into `src/SagraFacile.WebClient/Extensions/` unchanged — they are
pure C# helpers.

#### PWA Assets

**Scope: shell-only PWA.** The service worker caches the static WASM bundle and app shell so the
app is installable and loads instantly on repeat visits. When offline it shows a simple offline
page. No API response caching — all data requires connectivity.

| File | Description |
|------|-------------|
| `wwwroot/manifest.json` | Name: "SagraFacile", short_name: "Sagra", display: "standalone", icons (192/512 px), theme_color, background_color |
| `wwwroot/service-worker.js` | Development service worker (no-op / pass-through; generated by `blazorwasm --pwa` template) |
| `wwwroot/service-worker.published.js` | Production service worker: cache-first for static WASM assets, network-only for `/api/` and `/hubs/` routes, fallback to `offline.html` for navigation requests when offline |
| `wwwroot/offline.html` | Simple "You are offline" page; no dynamic content |
| `wwwroot/icon-192.png` | App icon 192×192 |
| `wwwroot/icon-512.png` | App icon 512×512 |
| `wwwroot/app.css` | Copy from `SagraFacile.Web/wwwroot/app.css` |
| `wwwroot/favicon.png` | Copy from `SagraFacile.Web/wwwroot/favicon.png` |
| `wwwroot/lib/bootstrap/` | Copy Bootstrap assets from `SagraFacile.Web/wwwroot/lib/bootstrap/` |

Key rule in `service-worker.published.js`:
```javascript
// Never cache API or hub traffic
if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/hubs/')) {
    return fetch(request); // always network
}
```

#### `Program.cs` (WebClient)

```csharp
// 1. Auth state
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());

// 2. HTTP clients — base address read from appsettings.json "ApiBaseUrl"
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddHttpClient<IEventsService, EventsService>(c =>
    c.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
// ... repeat for all services

// 3. Localization
var savedCulture = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "blazor-culture");
if (!string.IsNullOrEmpty(savedCulture))
{
    var culture = new CultureInfo(savedCulture);
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}
builder.Services.AddLocalization();
```

#### `wwwroot/appsettings.json` (WebClient)

No API base URL configuration is needed. The WebClient always runs same-origin with the backend,
so `builder.HostEnvironment.BaseAddress` is used directly. This file only needs settings that are
genuinely environment-agnostic (e.g., feature flags).

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

---

## Changes to `SagraFacile.Web`

### Files and directories to DELETE

```
src/SagraFacile.Web/Components/          ← entire directory
src/SagraFacile.Web/Resources/           ← moved to WebClient
src/SagraFacile.Web/Extensions/          ← moved to WebClient
```

Specific files to delete:
- `Components/App.razor`
- `Components/Routes.razor`
- `Components/RedirectToLogin.razor`
- `Components/_Imports.razor`
- `Components/Layout/*`
- `Components/Modals/*`
- `Components/Pages/*`

### `SagraFacile.Web.csproj` changes

Remove:
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.0.3" />
```
(SignalR.Client is only needed by browser-side pages now.)

Add:
```xml
<ProjectReference Include="..\SagraFacile.Contracts\SagraFacile.Contracts.csproj" />
```

Remove embedded resource for localization:
```xml
<!-- remove this -->
<EmbeddedResource Update="Resources/SharedResource.it.resx">...</EmbeddedResource>
```

### `Program.cs` changes

Remove:
```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddLocalization();
builder.Services.AddCascadingAuthenticationState();
// ...
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapPost("/logout", ...); // cookie logout — replaced by client-side JWT logout
```

Add (static file / SPA fallback):
```csharp
// Serve the pre-built Blazor WASM bundle placed in wwwroot/
app.UseDefaultFiles();   // serves index.html for /
app.UseStaticFiles();

// SPA fallback: return index.html for all non-API, non-hub routes so Blazor router works
app.MapFallbackToFile("index.html");
```

Update CORS policy name/scope to also cover the WebClient origin in development:
```csharp
options.AddPolicy("AppPolicy", policy =>
{
    policy.WithOrigins(
            builder.Configuration["AllowedOrigins"]?.Split(',') ?? Array.Empty<string>())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
});
```

Update `app.UseAntiforgery()` removal — the SPA → API pattern uses JWT; antiforgery is not needed
for the API controllers (they already use `[IgnoreAntiforgeryToken]`).

Keep/retain unchanged:
- JWT authentication & bearer configuration
- Authorization policies
- SignalR hub and notification pipeline
- Database migration & seed on startup
- `AllowHttp` flag
- All existing controllers

### New API Controllers

All new controllers go in `src/SagraFacile.Web/Controllers/` and follow the same pattern as the
existing `ReservationsController` (inject `IMediator`, use authorization policies, return typed DTOs
from `SagraFacile.Contracts`).

#### `EventsController.cs`

Route: `api/events`

| Method | Route | Auth Policy | CQRS | Returns |
|--------|-------|-------------|------|---------|
| GET | `/` | `AdminOrSupervisore` | `GetEvents.Query` | `List<EventDto>` |
| GET | `/active` | none | `GetActiveEvent.Query` | `EventDto?` |
| GET | `/{id:int}` | `AdminOrSupervisore` | `GetEventById.Query` *(new)* | `EventDto` |
| POST | `/` | `AdminOnly` | `CreateEvent.Command` | `EventDto` |
| PUT | `/{id:int}/activate` | `AdminOnly` | `ActivateEvent.Command` | 200 OK |
| GET | `/{id:int}/options` | `AdminOrSupervisore` | `GetEventAdditionalOptions.Query` | `EventAdditionalOptionsDto` |
| PUT | `/{id:int}/options` | `AdminOnly` | `UpdateEventAdditionalOptions.Command` | 200 OK |

#### `MenuController.cs`

Route: `api/events/{eventId:int}/menu`

| Method | Route | Auth Policy | CQRS |
|--------|-------|-------------|------|
| GET | `/` | none | `GetEventMenu.Query` |
| GET | `/details` | none | `GetMenuDetails.Query` |
| PUT | `/details` | `AdminOrSupervisore` | `UpdateMenuDetails.Command` |
| GET | `/categories` | none | `GetMenuCategories.Query` |
| POST | `/categories` | `AdminOrSupervisore` | `CreateMenuCategory.Command` |
| PUT | `/categories/{catId:int}` | `AdminOrSupervisore` | `UpdateMenuCategory.Command` |
| DELETE | `/categories/{catId:int}` | `AdminOrSupervisore` | `DeleteMenuCategory.Command` |
| POST | `/items` | `AdminOrSupervisore` | `CreateMenuItem.Command` |
| PUT | `/items/{itemId:int}` | `AdminOrSupervisore` | `UpdateMenuItem.Command` |
| DELETE | `/items/{itemId:int}` | `AdminOrSupervisore` | `DeleteMenuItem.Command` |

Route: `api/allergens`

| Method | Route | Auth Policy | CQRS |
|--------|-------|-------------|------|
| GET | `/` | none | `GetAllergens.Query` |

#### `ReservationsController.cs` — additions

Existing controller already handles writes. Add these read endpoints:

| Method | Route | Auth Policy | CQRS |
|--------|-------|-------------|------|
| GET | `/` | `Cassiere` | `GetReservations.Query` with query params: `eventId`, `status`, `page`, `pageSize` |
| GET | `/counters` | `Cassiere` | `GetCounters.Query` with `eventId` query param |
| GET | `/best-fit` | `Cassiere` | `GetBestFitReservation.Query` with `availableSeats` param |
| GET | `/report` | `AdminOrSupervisore` | `GetReservationReport.Query` with `eventId` param |

Also add:
| Method | Route | Auth Policy | CQRS |
|--------|-------|-------------|------|
| PUT | `/{id:int}` | `Cassiere` | `EditReservation.Command` |
| POST | `/{id:int}/party-complete` | `Cassiere` | `MarkPartyComplete.Command` |

#### `UsersController.cs`

Route: `api/users`

| Method | Route | Auth Policy | Description |
|--------|-------|-------------|-------------|
| GET | `/` | `AdminOnly` | List all users (`UserManager.Users`) |
| POST | `/` | `AdminOnly` | Create user (`UserManager.CreateAsync`) |
| GET | `/{id}/roles` | `AdminOnly` | Get roles for user (`UserManager.GetRolesAsync`) |
| POST | `/{id}/roles` | `AdminOnly` | Assign roles (remove all, add new ones) |

> `UsersController` directly injects `UserManager<ApplicationUser>` (same as current `Admin.razor`
> page). No CQRS command needed here since identity management is already infrastructure-level.

---

## Static File Serving Strategy

The WebClient is always served from the same origin as the Web backend. The Blazor WASM publish
output is copied into the backend's `wwwroot/` at build time. This ensures same-origin API/hub
access with no CORS configuration needed.

**Docker multi-stage build:**

```dockerfile
# Stage 1: Build WebClient
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS client-build
WORKDIR /src
COPY src/SagraFacile.Contracts ./SagraFacile.Contracts
COPY src/SagraFacile.WebClient ./SagraFacile.WebClient
RUN dotnet publish SagraFacile.WebClient/SagraFacile.WebClient.csproj \
    -c Release -o /client-publish

# Stage 2: Build Web backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/SagraFacile.Web/SagraFacile.Web.csproj \
    -c Release -o /app-publish

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app-publish .
COPY --from=client-build /client-publish/wwwroot ./wwwroot

ENTRYPOINT ["dotnet", "SagraFacile.Web.dll"]
```

**Local development workflow:**

Since the AppHost (Aspire) is not updated, developers run the two projects independently:

1. Publish the WebClient into the Web backend's `wwwroot/`:
   ```bash
   dotnet publish src/SagraFacile.WebClient -c Debug -o src/SagraFacile.Web/wwwroot --nologo
   ```
2. Run the backend (which now also serves the WASM files):
   ```bash
   dotnet run --project src/SagraFacile.Web
   ```
3. Open `https://localhost:5001` — the full app runs same-origin.

> **Tip for active UI development:** Add a `watch` script that re-publishes the WebClient on file
> change, or use `dotnet watch` on the WebClient with a post-build copy step. This replaces the
> Blazor Server hot-reload experience.

The `CORS` configuration in `Program.cs` can be narrowed to only allow SignalR WebSocket upgrades;
cross-origin REST calls are no longer needed.

---

## Docker / Deployment Changes

### `docker-compose.yml`, `docker-compose.lan.yml`, `docker-compose.release.yml`
No service changes needed — still a single `web` container. Remove the
`AllowedOrigins` env var from the compose files (cross-origin CORS no longer
required; the WASM client is served by the same container).

### `Dockerfile`
Replace the current single-stage build with the multi-stage build described in
the Static File Serving section above.

---

## Solution File Changes (`SagraFacile.slnx`)

Add two new projects:
```xml
<Project Path="src/SagraFacile.Contracts/SagraFacile.Contracts.csproj" />
<Project Path="src/SagraFacile.WebClient/SagraFacile.WebClient.csproj" />
```

Both go in the `/src/` folder group.

---

## Ordered Implementation Tasks for Junie

Complete these tasks in order. Each task is independently testable.

### Task 1 — Create `SagraFacile.Contracts`

1. Create `src/SagraFacile.Contracts/SagraFacile.Contracts.csproj` (net10.0, no deps).
2. Create all DTO and request model files listed in the Contracts section above.
3. Mirror shapes exactly from existing Application DTOs (check handler return types for exact field names/types).
4. Add project to `SagraFacile.slnx`.
5. Add `<ProjectReference>` to `SagraFacile.Web.csproj`.
6. Verify: `dotnet build src/SagraFacile.Contracts`.

### Task 2 — Expand `SagraFacile.Web` API surface

1. Move request models (`CreateReservationRequest`, `CallReservationRequest`, `CallAndSeatRequest`)
   from `ReservationsController.cs` inline records into `SagraFacile.Contracts`.
2. Create `EventsController.cs` with all endpoints from the table above.
3. Create `MenuController.cs` with all endpoints.
4. Create `UsersController.cs` with user management endpoints.
5. Add missing read+write endpoints to `ReservationsController.cs`.
6. All controllers map Application DTOs → Contracts DTOs in the response (or return Contracts DTOs
   directly if handler return type matches).
7. Verify: `dotnet build src/SagraFacile.Web` + run existing tests: `dotnet test`.

### Task 3 — Remove Blazor from `SagraFacile.Web`

1. Delete `src/SagraFacile.Web/Components/` directory.
2. Delete `src/SagraFacile.Web/Resources/` directory.
3. Delete `src/SagraFacile.Web/Extensions/` directory.
4. Update `SagraFacile.Web.csproj`: remove `SignalR.Client` package ref; remove localization
   embedded resource; remove Blazor-specific items.
5. Update `Program.cs`: remove Razor components, antiforgery, localization middleware, cookie logout
   endpoint; add `UseDefaultFiles()`, `UseStaticFiles()`, `MapFallbackToFile("index.html")`.
6. Create an empty placeholder `wwwroot/index.html` so the server starts cleanly (replaced by WASM
   output in production).
7. Verify: `dotnet build src/SagraFacile.Web`.

### Task 4 — Scaffold `SagraFacile.WebClient`

1. Create project with `dotnet new blazorwasm --pwa -n SagraFacile.WebClient
   -o src/SagraFacile.WebClient`.
2. Add project reference to `SagraFacile.Contracts`.
3. Add `Microsoft.AspNetCore.SignalR.Client` and `Microsoft.AspNetCore.Components.Authorization`
   package refs.
4. Add to solution file.
5. Verify: `dotnet build src/SagraFacile.WebClient`.

### Task 5 — Implement authentication in WebClient

1. Create `Auth/TokenStorageService.cs` (JS interop for localStorage).
2. Create `Auth/JwtAuthStateProvider.cs` (parse JWT claims, implement `AuthenticationStateProvider`).
3. Create `Auth/AuthService.cs` (`LoginAsync` / `LogoutAsync`).
4. Register in `Program.cs`.
5. Create `AuthorizationMessageHandler.cs` delegating handler.
6. Create `wwwroot/appsettings.json` with `ApiBaseUrl`.
7. Verify: app compiles and auth state provider is resolvable.

### Task 6 — Implement typed HTTP client services

1. Create all service classes listed in the Typed HTTP Client Services section.
2. Register all services in `Program.cs` with the `AuthorizationMessageHandler`.
3. Verify: services resolve without errors (`dotnet build`).

### Task 7 — Migrate Blazor components to WebClient

1. Copy `src/SagraFacile.Web/Components/` → `src/SagraFacile.WebClient/Components/`.
2. Copy `src/SagraFacile.Web/wwwroot/app.css`, `favicon.png`, `lib/` → WebClient `wwwroot/`.
3. Copy `src/SagraFacile.Web/Resources/` → `src/SagraFacile.WebClient/Resources/`.
4. Copy `src/SagraFacile.Web/Extensions/` → `src/SagraFacile.WebClient/Extensions/`.
5. Apply global changes (remove `@rendermode`, replace `IMediator` injections) to all components.
6. Apply page-specific changes from the migration table above.
7. Fix `SignalR` token injection in all pages that build `HubConnection`.
8. Configure localization in `Program.cs` (culture from localStorage or browser).
9. Update `_Imports.razor` with new namespace imports.
10. Verify: `dotnet build src/SagraFacile.WebClient`.

### Task 8 — PWA assets

1. Update `wwwroot/manifest.json` with app name, icons, theme.
2. Add icon files (192×512 PNG) to `wwwroot/`.
3. Verify service worker is included in publish output (`dotnet publish`).

### Task 9 — Docker multi-stage build

1. Update `Dockerfile` to the multi-stage pattern described above.
2. Update `docker-compose.yml` / `docker-compose.lan.yml` / `docker-compose.release.yml` as
   needed.
3. Verify: `docker build -t sagrafacile .` completes successfully.

### Task 10 — End-to-end smoke test

1. Run the multi-container stack: `docker compose up -d`.
2. Open `http://localhost:5000` → verify WASM bundle loads (Network tab shows `dotnet.wasm`).
3. Log in → verify JWT stored in localStorage.
4. Navigate all pages → confirm no 401 / 404 API errors.
5. Open Receptionist page with SignalR → create a reservation → confirm real-time updates arrive.
6. Run existing test suites: `dotnet test`.

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| DTO shape mismatch between Application and Contracts causing JSON deserialization failures | Mirror Application DTO field names exactly; write a simple integration test that round-trips one request per controller |
| Blazor WASM `IStringLocalizer` not finding `.resx` files (satellite assemblies not loaded) | Follow Blazor WASM i18n docs: set `BlazorWebAssemblyLoadAllGlobalizationData=true` in .csproj; ensure culture is set before `RunAsync` |
| Large initial bundle size (dotnet.wasm ~2MB gzipped) | Enable Brotli/gzip compression in Web backend; acceptable for LAN/intranet deployment |
| Dev iteration is slower — no hot-reload like Blazor Server | Run `dotnet watch publish` on WebClient piped into Web's wwwroot; or accept a manual re-publish step; document clearly for team |
| SignalR token transport — `AccessTokenProvider` callback vs query param | The server already handles query param in `JwtBearerEvents.OnMessageReceived`; using `AccessTokenProvider` in SignalR client triggers the same path |
| Cookie-based logout endpoint (`POST /logout`) removed | JWT expiry handles session end; client deletes token from localStorage on logout — no server endpoint needed |
| `Admin.razor` used `UserManager` directly — no CQRS command exists | `UsersController` injects `UserManager<ApplicationUser>` directly (same pattern as existing `AuthController`) |
| `Weather.razor` placeholder page | Delete it — it has no production purpose |
| `MapFallbackToFile("index.html")` intercepts unmatched `/api/` routes returning 404 | Ensure fallback is registered **after** `MapControllers()` and `MapHub()`; ASP.NET Core endpoint routing resolves known routes first, fallback only fires for unmatched paths |
