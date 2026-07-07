# Plan: Standardize Menu DTOs — Contracts as Single Source of Truth

## Context

`SagraFacile.Contracts` is the shared API surface between `SagraFacile.Web` and `SagraFacile.WebClient`. Menu management has accumulated several DTO inconsistencies that cause **real compile errors** today:

1. **`MenuItemDto` is missing admin fields** — `CategoryId`, `IsAvailable`, `DisplayOrder`, `CategoryName` exist in `Application.GetEventMenu.MenuItemDto` but NOT in `Contracts.Menu.MenuItemDto`. `MenuManagement.razor` line 380 is an incomplete expression (`CategoryId = item.,`) and line 204 references `item.IsAvailable`, line 222 uses `item.PriceInCents` — all compile errors.

2. **`SortOrder` vs `DisplayOrder` naming clash** — Contracts uses `SortOrder`; Domain/Application uses `DisplayOrder`. `MenuManagement.razor` line 74 writes `@c.DisplayOrder` on a `MenuCategoryDto` (which has `SortOrder`) → compile error.

3. **`Warning` vs `WarningMessage` inconsistency within Contracts** — `MenuDetailsDto` uses `WarningMessage`; `UpdateMenuDetailsRequest`/`UpdateMenuDetailsResponse` use `Warning`.

4. **Duplicate inline DTOs in Application** — `AllergenDto`, `MenuItemDto`, `CategoryDto`, `MenuDetailsDto` are all duplicated in Application handlers and Contracts, forcing manual mapping in `MenuController`.

5. **Wrong `MenuService` return types** — All write methods return `Task<int>` but controllers return structured JSON objects. These fail at runtime.

6. **`MenuManagement.razor` calls `IMediator` directly** for 5 write operations — won't work in WASM render mode.

7. **`MenuCategoryDto.EventId` is always 0** — `MenuCategory` domain entity has no `EventId`; the field was hardcoded.

---

## Goal

Establish `Contracts.Menu` as the single source of truth for all menu API types — eliminate duplicate definitions, fix naming, remove controller mapping boilerplate, fix `MenuService` response types, fix `MenuManagement.razor`.

---

## Task 1 — Contracts: enrich and fix types

**`src/SagraFacile.Contracts/Menu/MenuItemDto.cs`** — Modify  
Add admin fields; add write response types:
```csharp
public record MenuItemDto(
    int Id, string Name, string Description, int PriceCents,
    int CategoryId, string CategoryName, int DisplayOrder, bool IsAvailable,
    List<AllergenDto> Allergens);

public record CreateMenuItemResponse(int Id, string Name);
public record UpdateMenuItemResponse(bool Success, string Message);
public record DeleteMenuItemResponse(bool Success, string Message);
```

**`src/SagraFacile.Contracts/Menu/Category.cs`** — Modify  
- Rename `SortOrder` → `DisplayOrder` everywhere  
- Remove `EventId` from `MenuCategoryDto` and `CreateMenuCategoryRequest` (domain is global, no EventId)  
- Rename `Status` → `Success` in response types (align with Application naming):
```csharp
public record MenuCategoryDto(int Id, string Name, int DisplayOrder, List<MenuItemDto> Items);
public record CreateMenuCategoryRequest(string Name, int DisplayOrder);
public record CreateMenuCategoryResponse(int Id, string Name);
public record UpdateMenuCategoryRequest(string Name, int DisplayOrder);
public record UpdateMenuCategoryResponse(bool Success, string Message);
public record DeleteMenuCategoryResponse(bool Success, string Message);
```

**`src/SagraFacile.Contracts/Menu/MenuDetailsDto.cs`** — Modify  
Rename `Warning` → `WarningMessage` in request/response:
```csharp
public record MenuDetailsDto(string? Header, string? Footer, string? WarningMessage);
public record UpdateMenuDetailsRequest(string? Header, string? Footer, string? WarningMessage);
public record UpdateMenuDetailsResponse(string? Header, string? Footer, string? WarningMessage);
```

---

## Task 2 — Application: return Contracts types, drop inline DTOs

Add `using SagraFacile.Contracts.Menu;` to each handler.

**`GetAllergens.cs`** — Remove local `AllergenDto`, use `Contracts.Menu.AllergenDto`.

**`GetEventMenu.cs`** — Remove local `MenuItemDto` and `AllergenDto`, use Contracts types. Map `PriceInCents` (domain) → `PriceCents` (Contracts) and add the new fields:
```csharp
new MenuItemDto(i.Id, i.Name, i.Description, i.PriceInCents,
    i.CategoryId, i.Category?.Name ?? "", i.DisplayOrder, i.IsAvailable,
    i.MenuItemAllergens.Select(mia => new AllergenDto(...)).ToList())
```

**`GetMenuDetails.cs`** — Remove local `MenuDetailsDto`, use Contracts type. Drop `EventId` from mapping (no longer in the Contracts DTO):
```csharp
new MenuDetailsDto(entity.Header, entity.Footer, entity.WarningMessage)
```

**`GetMenuCategories.cs`** — Remove local `CategoryDto`, use `Contracts.Menu.MenuCategoryDto`. Keep `Query()` with no parameters (categories are global). Update mapping:
```csharp
new MenuCategoryDto(c.Id, c.Name, c.DisplayOrder, [])
```

---

## Task 3 — Controller: remove mapping boilerplate

**`src/SagraFacile.Web/Controllers/MenuController.cs`** — Modify

| Method | Change |
|---|---|
| `GetMenu` | `return Ok(menuResult.Items)` — remove manual Select mapping |
| `GetMenuDetails` | `return Ok(result.Details)` — remove manual construction |
| `GetCategories` | `return Ok(result.Categories)` — remove manual Select |
| `GetAllergens` | `return Ok(result.Allergens)` — remove manual Select |
| `CreateCategory` | `return Ok(new CreateMenuCategoryResponse(r.Id, r.Name))` |
| `UpdateCategory` | `return Ok(new UpdateMenuCategoryResponse(r.Success, r.Message))` |
| `DeleteCategory` | `return Ok(new DeleteMenuCategoryResponse(r.Success, r.Message))` |
| `CreateItem` | `return Ok(new CreateMenuItemResponse(r.Id, r.Name))` |
| `UpdateItem` | `return Ok(new UpdateMenuItemResponse(r.Success, r.Message))` |
| `DeleteItem` | `return Ok(new DeleteMenuItemResponse(r.Success, r.Message))` |
| `UpdateMenuDetails` | `request.Warning` → `request.WarningMessage` (after Task 1 rename) |

---

## Task 4 — WebClient: fix service and UI

**`src/SagraFacile.WebClient/Services/IMenuService.cs`** — Modify  
Replace `Task<int>` return types with proper Contracts response types:
```csharp
Task<CreateMenuCategoryResponse> CreateCategoryAsync(...);
Task<UpdateMenuCategoryResponse> UpdateCategoryAsync(...);
Task<DeleteMenuCategoryResponse> DeleteCategoryAsync(...);
Task<CreateMenuItemResponse> CreateItemAsync(...);
Task<UpdateMenuItemResponse> UpdateItemAsync(...);
Task<DeleteMenuItemResponse> DeleteItemAsync(...);
Task<UpdateMenuDetailsResponse> UpdateMenuDetailsAsync(...);
```

**`src/SagraFacile.WebClient/Services/MenuService.cs`** — Modify  
Replace `ReadFromJsonAsync<int>()` with `ReadFromJsonAsync<{ResponseType}>()` for all write operations.

**`src/SagraFacile.WebClient/Components/Pages/MenuManagement.razor`** — Modify  

Compile error fixes:
- Line 222: `item.PriceInCents` → `item.PriceCents`  
- Line 380: `CategoryId = item.,` → `CategoryId = item.CategoryId,`
- `item.IsAvailable`, `item.CategoryId` references — now valid after MenuItemDto is enriched

Replace all 5 `Mediator.SendAsync(...)` calls with `MenuService` equivalents:

| Old | New |
|---|---|
| `Mediator.SendAsync(new CreateMenuItem.Command(EventId, ...))` | `MenuService.CreateItemAsync(EventId, new CreateMenuItemRequest(...))` |
| `Mediator.SendAsync(new DeleteMenuItem.Command(id))` | `MenuService.DeleteItemAsync(EventId, id)` |
| `Mediator.SendAsync(new UpdateMenuCategory.Command(...))` | `MenuService.UpdateCategoryAsync(EventId, editingCatId.Value, new UpdateMenuCategoryRequest(...))` |
| `Mediator.SendAsync(new CreateMenuCategory.Command(...))` | `MenuService.CreateCategoryAsync(EventId, new CreateMenuCategoryRequest(...))` |
| `Mediator.SendAsync(new DeleteMenuCategory.Command(id))` | `MenuService.DeleteCategoryAsync(EventId, id)` |

Fix type references:
- `EditCategory(GetMenuCategories.CategoryDto c)` → `EditCategory(MenuCategoryDto c)`
- `GetCategoryName(GetMenuCategories.CategoryDto c)` → `GetCategoryName(MenuCategoryDto c)`
- `@c.DisplayOrder` — valid after Contracts rename

---

## Acceptance Criteria

1. `dotnet build` → 0 errors, 0 warnings in the changed files.
2. `MenuManagement.razor` has no references to Application-layer types.
3. `GET /api/events/{id}/menu` response JSON includes `categoryId`, `categoryName`, `displayOrder`, `isAvailable`.
4. `GET /api/events/{id}/menu/categories` response uses `displayOrder` (not `sortOrder`).
5. `GET /api/events/{id}/menu/details` response uses `warningMessage` (not `warning`).
6. `POST /api/events/{id}/menu/categories` returns `{ "id": N, "name": "..." }` (not empty body).
7. All write operations in `MenuController` return Contracts response types.
8. Application handlers have no local DTO record definitions for `AllergenDto`, `MenuItemDto`, `CategoryDto`, `MenuDetailsDto`.

## Verification

```bash
dotnet build
dotnet test
```

Manual: load `/events/{id}/menu` admin page, perform full CRUD on categories and items, edit menu appearance — all operations complete and refresh correctly.

## Risks

| Risk | Mitigation |
|---|---|
| External API clients relying on old JSON field names (`sortOrder`, `warning`) | Field renames are breaking changes for raw HTTP clients. Acceptable for an internal project. |
| Cache holds stale type definitions | `IMenuCacheService` stores `GetEventMenu.Result` — after Task 2, `Result` holds Contracts types. Cache key/signature unchanged; no impact. |
| `UpdateMenuDetailsRequest` field order change | Only caller is `MenuManagement.razor` (updated in Task 4). JSON deserialization is name-based, not positional. |
