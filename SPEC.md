# Spec: Get Events Use Case (Event bounded context)

## Objective

Add a "get events" capability to the Event bounded context so callers (the
Scalar UI, the BFF once it's wired up, or any HTTP client) can list events
instead of only fetching one by id.

- **Get all events** — no filters, returns every event that is not yet over.
- **Get events in a timeframe** — an optional `startDate` and/or optional
  `endDate` narrow the result to events whose own `[StartDate, EndDate]`
  window overlaps the requested window.
- **Past events are never returned.** An event is "past" once it has
  finished (`EndDate < now`). This holds even if a caller explicitly asks
  for a past window — the lower bound is always clamped forward to now.
- **Defaults:** `startDate` defaults to "now" when omitted. `endDate`
  defaults to "no upper bound" (infinite) when omitted.

This is scoped to the `Event` bounded context only (`Event.Domain`,
`Event.Application`, `Event.Infrastructure`, `Event.Api`). It does not touch
Agenda or the BFF.

## Tech Stack

Matches the rest of the repo — no new dependencies:

- .NET 10 (`net10.0`), C#, `Nullable` + `ImplicitUsings` enabled
- ASP.NET Core Web API (`Event.Api`), controller-based (`[ApiController]`)
- EF Core + Npgsql (`Event.Infrastructure`), code-first migrations
- xUnit for tests; Testcontainers.PostgreSql for repository integration tests

## Commands

```bash
dotnet build Event/Event.sln
dotnet test Event/Event.sln
dotnet run --project Event/Event.Api/Event.Api.csproj
# or: ./launch-event-api.sh
```

OpenAPI/Scalar at `/openapi/v1.json` and `/scalar/v1` once running.

## Project Structure

New/changed files, following the existing layering:

```
Event/Event.Domain/Ports/IEventRepository.cs          -> new GetAllAsync method signature
Event/Event.Application/UseCases/GetEventsUseCase.cs   -> new use case (clamping + defaults live here)
Event/Event.Application/EventApplicationServiceCollectionExtensions.cs -> register GetEventsUseCase
Event/Event.Infrastructure/EventRepository.cs          -> implement GetAllAsync (overlap query)
Event/Event.Api/Contracts/GetEventsRequest.cs          -> [FromQuery] startDate/endDate binding (new)
Event/Event.Api/Controllers/EventsController.cs        -> new GET /api/events endpoint
Event/Event.Domain.Tests/...                            -> n/a (no new domain invariants)
Event/Event.Application.Tests/GetEventsUseCaseTests.cs -> new (clamping/default logic — new test project if one doesn't exist)
Event/Event.Infrastructure.Tests/EventRepositoryTests.cs -> extend with GetAllAsync cases
```

No changes to `Event.Domain/Event.cs` — filtering is a query concern, not a
domain invariant, so it belongs in the use case (defaults/clamping) and the
repository (the actual overlap query), matching how `GetByIdAsync` is
already split between `IEventRepository` and `EventRepository`.

## Code Style

Follow the existing use-case/repository pattern exactly. New repository
method:

```csharp
// Event.Domain/Ports/IEventRepository.cs
public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Event>> GetAllAsync(DateTime start, DateTime? end, CancellationToken ct);
    Task AddAsync(Event @event, CancellationToken ct);
}
```

New use case — defaulting and clamping happen here, not in the controller
or the repository, so the repository can stay a dumb overlap query:

```csharp
// Event.Application/UseCases/GetEventsUseCase.cs
public sealed class GetEventsUseCase(IEventRepository eventRepository, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<DomainEvent>> ExecuteAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var effectiveStart = startDate is null ? now : Max(startDate.Value, now);

        return eventRepository.GetAllAsync(effectiveStart, endDate, ct);
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
```

Repository overlap query — event is included when it overlaps
`[effectiveStart, endDate ?? infinite)` and is not yet finished:

```csharp
// Event.Infrastructure/EventRepository.cs
public async Task<IReadOnlyList<DomainEvent>> GetAllAsync(DateTime start, DateTime? end, CancellationToken ct)
{
    var query = dbContext.Events
        .AsNoTracking()
        .Where(e => e.EndDate > start);

    if (end is not null)
        query = query.Where(e => e.StartDate < end.Value);

    var entities = await query.OrderBy(e => e.StartDate).ToListAsync(ct);
    return entities.Select(ToDomain).ToList();
}
```

Controller — plain query-string binding, same try/catch shape as
`CreateEvent`:

```csharp
[HttpGet]
public async Task<IActionResult> GetEvents([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct)
{
    var events = await getEventsUseCase.ExecuteAsync(startDate, endDate, ct);
    return Ok(events.Select(EventResponse.FromDomain));
}
```

`TimeProvider` (built-in, `System.TimeProvider`) is injected rather than
calling `DateTime.UtcNow` directly so the "now" clamp is testable without
wall-clock flakiness — register `TimeProvider.System` in DI.

## Testing Strategy

Same three levels the repo already uses per bounded context:

- **Application unit tests** (new `Event.Application.Tests` project, xUnit,
  fake `IEventRepository` + fake/fixed `TimeProvider`):
  - no filters given -> repository called with `(now, null)`
  - only `startDate` (future) given -> repository called with `(startDate, null)`
  - only `endDate` given -> repository called with `(now, endDate)`
  - `startDate` in the past -> repository called with `(now, ...)` (clamped)
  - both given, `startDate` in the future -> passed through unchanged
- **Infrastructure integration tests** (extend
  `Event.Infrastructure.Tests/EventRepositoryTests.cs`, Testcontainers
  Postgres, matching existing style):
  - event fully before the window -> excluded
  - event fully after the window -> included (no upper bound) / excluded (window ends before it starts)
  - event overlapping the start boundary, end boundary, and fully containing the window -> included
  - event already ended (`EndDate < start`) -> excluded even with no explicit filters
  - `endDate = null` -> no upper bound applied
  - results ordered by `StartDate` ascending
- No new domain tests — `Event.Domain/Event.cs` is unchanged.

Run via `dotnet test Event/Event.sln`.

## Boundaries

- **Always do:** keep the "no past events" rule enforced in the use case
  (not trusted to callers), keep `IEventRepository`'s new method
  `AsNoTracking()` like `GetByIdAsync`, add tests for every acceptance
  criterion below before considering this done.
- **Ask first:** changing `EventResponse`'s shape (e.g. adding pagination,
  a total count, or a different date format), adding a new NuGet package,
  changing the existing `GetByIdAsync`/`AddAsync` contracts, touching
  Agenda or BFF.
- **Never do:** allow `endDate < startDate` to silently return everything
  (validate and return `400 Bad Request` instead), let the controller or
  repository decide what "now" is independently of the use case (single
  source of truth for the clamp), remove or weaken existing tests.

## Success Criteria

- `GET /api/events` with no query params returns all non-past events
  (`EndDate >= now`), ordered by `StartDate` ascending.
- `GET /api/events?startDate=...` returns events overlapping
  `[max(startDate, now), infinity)`.
- `GET /api/events?endDate=...` returns events overlapping `[now, endDate)`.
- `GET /api/events?startDate=...&endDate=...` returns events overlapping
  `[max(startDate, now), endDate)`.
- `GET /api/events?startDate=...&endDate=...` where `endDate < startDate`
  returns `400 Bad Request`.
- A `startDate` in the past is silently clamped to now — never returns
  events entirely before now, never errors.
- An event that has already ended (`EndDate < now`) is never returned,
  regardless of filters.
- All new behavior covered by application-layer unit tests and
  infrastructure integration tests; `dotnet test Event/Event.sln` passes.

## Open Questions

- Past-event definition (`EndDate < now`), overlap semantics, and
  past-`startDate` clamping were confirmed with the user before writing
  this spec.
- **Assumption, not yet confirmed:** `endDate < startDate` is treated as a
  validation error (`400 Bad Request`) rather than silently returning an
  empty list or swapping the two dates. Flagged for review below.
