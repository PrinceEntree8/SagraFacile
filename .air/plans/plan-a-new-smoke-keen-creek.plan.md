# Smoke Test Project: SagraFacile.Tests.Smoke

## Context

The project has a reservation system exposed via REST (`ReservationsController`) and SignalR (`ReservationHub`). Real-world usage involves many concurrent viewers (displays showing queue status) alongside a steady stream of reservation lifecycle operations. This smoke test validates that the system handles the concurrent load without SignalR delivery degradation or API errors.

---

## Goal

Create a new NBomber-based smoke test project under `tests/SagraFacile.Tests.Smoke/` that: (1) maintains up to 1000 persistent SignalR connections listening for `ReservationStatusChanged`, and (2) drives reservation lifecycle traffic at ~20 creates/min, each reservation going through create → call → seat/void.

---

## Approach

Use NBomber v5 with two parallel scenarios:
- **`signalr_listeners`** — `KeepConstant(1000)` virtual users, each opening a persistent `HubConnection`, joining the configured group, and recording received notifications as custom metrics. Per-VU connection is created once and stored in `ctx.Data`.
- **`reservation_lifecycle`** — `Inject(20, 1 min)` drives 20 iterations/min; each iteration executes the full create → delay → call → delay → seat/void pipeline as tracked sub-steps. No external coordinator needed; the 2–5 s delays naturally spread operations over time.

The ~50 rpm status-change approximation is achieved because multiple in-flight reservations are concurrently in the call/seat stage while new ones are being created; the actual measured throughput is reported by NBomber rather than hardcoded.

Auth is performed once before the runner starts; the JWT bearer token is shared across all HTTP clients.

---

## File Changes

| File | Action | Purpose |
|------|--------|---------|
| `tests/SagraFacile.Tests.Smoke/SagraFacile.Tests.Smoke.csproj` | Create | Project definition, NBomber + SignalR packages |
| `tests/SagraFacile.Tests.Smoke/appsettings.json` | Create | Local defaults; env vars override |
| `tests/SagraFacile.Tests.Smoke/TestConfig.cs` | Create | Typed config model |
| `tests/SagraFacile.Tests.Smoke/AuthService.cs` | Create | Calls `POST /api/auth/login`, returns bearer token |
| `tests/SagraFacile.Tests.Smoke/Program.cs` | Create | Entry point: loads config, runs auth, builds and starts NBomber runner |
| `tests/SagraFacile.Tests.Smoke/Scenarios/SignalRListenerScenario.cs` | Create | 1000 persistent SignalR connections scenario |
| `tests/SagraFacile.Tests.Smoke/Scenarios/ReservationLifecycleScenario.cs` | Create | Reservation create → call → seat/void scenario |

---

## Implementation Steps

### Task 1 — Project scaffolding

**Step 1.1** Create `SagraFacile.Tests.Smoke.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NBomber" Version="5.*" />
    <PackageReference Include="NBomber.Http" Version="5.*" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.*" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.*" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="10.*" />
  </ItemGroup>
  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

**Step 1.2** Create `appsettings.json` with defaults:
```json
{
  "SmokeTest": {
    "BaseUrl": "http://localhost:5000",
    "Username": "admin",
    "Password": "admin",
    "EventId": 1,
    "SignalRGroup": "event-1",
    "ListenerCount": 1000,
    "DurationSeconds": 300,
    "CreateRatePerMinute": 10
  }
}
```

**Step 1.3** Create `TestConfig.cs`:
```csharp
public record TestConfig(
    string BaseUrl,
    string Username,
    string Password,
    int EventId,
    string SignalRGroup,
    int ListenerCount,
    int DurationSeconds,
    int CreateRatePerMinute
);
```

### Task 2 — Authentication

**Step 2.1** Create `AuthService.cs`:
- `POST {BaseUrl}/api/auth/login` with `{ username, password }`
- Deserializes `LoginResponse` (returns `.Token` string)
- Throws on non-200 with a clear message

### Task 3 — SignalR listener scenario

**Step 3.1** Create `Scenarios/SignalRListenerScenario.cs`:
```csharp
Scenario.Create("signalr_listeners", async ctx => {
    if (!ctx.Data.ContainsKey("hub")) {
        var conn = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/reservations")
            .WithAutomaticReconnect()
            .Build();
        
        conn.On<object>("ReservationStatusChanged", _ =>
            Interlocked.Increment(ref notificationsReceived));
        
        await conn.StartAsync(ctx.CancellationToken);
        await conn.InvokeAsync("JoinReservationGroup", groupName, ctx.CancellationToken);
        ctx.Data["hub"] = conn;
    }

    // keep-alive tick — measure connection health
    await Task.Delay(500, ctx.CancellationToken);
    return Response.Ok();
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: listenerCount, during: duration)
)
.WithClean(async ctx => {
    if (ctx.Data.TryGetValue("hub", out var obj) && obj is HubConnection c)
        await c.DisposeAsync();
});
```

Note: `notificationsReceived` is a shared `long` reported via NBomber custom metrics at the end.

### Task 4 — Reservation lifecycle scenario

**Step 4.1** Create `Scenarios/ReservationLifecycleScenario.cs`:

```csharp
Scenario.Create("reservation_lifecycle", async ctx => {
    // Step: Create
    var createStep = await Step.Run("create", ctx, async () => {
        var resp = await http.PostAsJsonAsync("/api/reservations",
            new { eventId, customerName = $"Guest-{ctx.ScenarioInfo.ThreadId}",
                  partySize = Random.Shared.Next(1, 6) });
        if (!resp.IsSuccessStatusCode)
            return Response.Fail(statusCode: (int)resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<CreateResult>();
        ctx.Data["reservationId"] = body!.Id;
        return Response.Ok(sizeBytes: (int)(resp.Content.Headers.ContentLength ?? 0));
    });
    if (createStep.IsError) return Response.Fail();

    await Task.Delay(Random.Shared.Next(2000, 5001), ctx.CancellationToken);

    // Step: Call
    var reservationId = (int)ctx.Data["reservationId"];
    var callStep = await Step.Run("call", ctx, async () => {
        var resp = await http.PostAsJsonAsync($"/api/reservations/{reservationId}/call",
            new { calledBy = "SmokeTest" });
        return resp.IsSuccessStatusCode ? Response.Ok() : Response.Fail((int)resp.StatusCode);
    });
    if (callStep.IsError) return Response.Fail();

    await Task.Delay(Random.Shared.Next(2000, 5001), ctx.CancellationToken);

    // Step: Seat or Void (1% void)
    var isVoid = Random.Shared.Next(100) == 0;
    var finalStep = await Step.Run(isVoid ? "void" : "seat", ctx, async () => {
        var resp = isVoid
            ? await http.DeleteAsync($"/api/reservations/{reservationId}")
            : await http.PostAsJsonAsync($"/api/reservations/{reservationId}/seat", new { });
        return resp.IsSuccessStatusCode ? Response.Ok() : Response.Fail((int)resp.StatusCode);
    });

    return finalStep.IsError ? Response.Fail() : Response.Ok();
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: createRate, interval: TimeSpan.FromMinutes(1),
                             during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: createRate, interval: TimeSpan.FromMinutes(1),
                      during: duration - TimeSpan.FromSeconds(30))
);
```

HTTP client is shared, with `Authorization: Bearer {token}` set on all requests.

### Task 5 — Entry point

**Step 5.1** Create `Program.cs`:
1. Build `IConfiguration` from `appsettings.json` + env vars (prefix `SMOKETEST__` for overrides)
2. Bind `TestConfig`
3. Call `AuthService.LoginAsync(config)` → get JWT token
4. Build shared `HttpClient` with `BaseAddress` and bearer header
5. Build and run NBomber runner:
```csharp
NBomberRunner
    .RegisterScenarios(
        SignalRListenerScenario.Create(config, token),
        ReservationLifecycleScenario.Create(config, httpClient)
    )
    .WithTestName("SagraFacile Smoke Test")
    .WithReportFolder("reports")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
    .Run();
```

---

## Acceptance Criteria

1. `dotnet run --project tests/SagraFacile.Tests.Smoke` exits cleanly against a running local instance.
2. NBomber HTML report shows `create`, `call`, `seat`, and `void` steps with p99 latency and zero unexpected errors.
3. `signalr_listeners` scenario maintains close to the configured `ListenerCount` connections throughout the run (NBomber shows active VUs staying constant).
4. The `notificationsReceived` counter in the report is ≥ (number of create + call + seat/void operations × 0.9), confirming SignalR delivery is not dropping messages under load.
5. No unhandled exceptions appear in console output.

---

## Verification Steps

1. Start the app: `cd src/SagraFacile.Web && dotnet run`
2. Ensure a valid event exists with a known ID and set it in `appsettings.json` (or `SMOKETEST__EventId` env var)
3. Set credentials: `SMOKETEST__Username=cassiere SMOKETEST__Password=...`
4. Run: `dotnet run --project tests/SagraFacile.Tests.Smoke`
5. Open `tests/SagraFacile.Tests.Smoke/reports/*.html` after the run
6. Verify in the report:
   - `signalr_listeners` active VUs ≈ 1000
   - `create` step success rate = 100%
   - `call` + `seat` step success rate = 100%
   - `void` step has ≤ 1% of total lifecycle iterations

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| 1000 SignalR connections exhaust OS socket limit on dev machine | Document `ulimit -n 4096` requirement; default to configurable `ListenerCount` so it can be reduced for local testing |
| `ctx.Data` dictionary in NBomber v5 — verify exact API shape | Check NBomber v5 release notes; fallback: use `ConcurrentDictionary<int, int>` keyed by `ctx.ScenarioInfo.ThreadId` |
| JWT token expires mid-run (8-hour expiry) | Not an issue for a 5-min test; document for longer runs |
| `Step.Run` sub-step API changed between NBomber versions | Pin to a specific minor version (e.g., `5.5.*`) to avoid drift |
| Concurrent creates may hit sequence-number unique-constraint retries under high load | The `CreateReservation` handler already retries up to 5 times; if failures appear, reduce `CreateRatePerMinute` |
