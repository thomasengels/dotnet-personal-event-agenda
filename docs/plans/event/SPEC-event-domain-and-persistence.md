# Spec: Event Domain & Persistence

Module id: `event-domain-and-persistence` (single capability, no capability map needed).
Supersedes/derives from: `docs/plans/event/event-domain-and-persistence.md` (confirmed interview intent).

## Objective

Replace the `Event` bounded context's hardcoded stub controller data with a
real, validated domain model backed by PostgreSQL, structured as Ports &
Adapters (Hexagonal Architecture).

**User:** the repo owner, evolving `Event.Api` from scaffold to a real service.

**Success looks like:** `Event.Api` can create and read `Event`s that are
persisted in PostgreSQL via EF Core migrations, with all domain invariants
(valid name, description length, date ordering) enforced by the domain layer
itself — invalid `Event`s cannot be constructed, let alone persisted.

## Tech Stack

- .NET 10 (`net10.0`), `Nullable` + `ImplicitUsings` enabled — matches every
  existing project in the repo.
- ASP.NET Core Web API (`Event.Api`) — unchanged.
- EF Core 10.x + `Npgsql.EntityFrameworkCore.PostgreSQL` (Infrastructure only).
- `Microsoft.EntityFrameworkCore.Design` for `dotnet ef` migration tooling.
- PostgreSQL (version matches whatever the eventual Azure Container Apps /
  Postgres target uses — not yet provisioned; local dev via `docker-compose.yml`,
  see Project Structure).
- `Testcontainers.PostgreSql` (test-only, `Event.Infrastructure.Tests`).

## Commands

```bash
# Build the Event bounded context (all 4 projects)
dotnet build Event/Event.sln

# Run the API
dotnet run --project Event/Event.Api/Event.Api.csproj
# or: ./launch-event-api.sh

# Add a migration (run from repo root)
dotnet ef migrations add <Name> \
  --project Event/Event.Infrastructure/Event.Infrastructure.csproj \
  --startup-project Event/Event.Api/Event.Api.csproj

# Apply migrations to the configured database
dotnet ef database update \
  --project Event/Event.Infrastructure/Event.Infrastructure.csproj \
  --startup-project Event/Event.Api/Event.Api.csproj

# Run Event's tests (once test projects exist — see Testing Strategy)
dotnet test Event/Event.sln
```

## Project Structure

```
docker-compose.yml               (new — root-level, single `postgres` service, shared across future services)
Event/
  Event.sln
  Event.Api/                    (existing — Web API host, DI composition root)
    Controllers/EventsController.cs
    Program.cs
  Event.Application/            (new)
    UseCases/                   e.g. CreateEvent, GetEventById
  Event.Domain/                 (existing, extended)
    Event.cs                    the Event aggregate root
    ValueObjects/Address.cs
    Ports/IEventRepository.cs   output port interface
  Event.Infrastructure/         (new)
    EventDbContext.cs
    Entities/EventEntity.cs     EF-mapped persistence entity (distinct from domain Event)
    Repositories/EventRepository.cs   implements IEventRepository (outbound adapter)
    Migrations/                 EF Core migrations
```

- `Event.Domain` has no project references (defines the port).
- `Event.Application` references `Event.Domain` only.
- `Event.Infrastructure` references `Event.Domain` (to implement the port) —
  not `Event.Application`.
- `Event.Api` references `Event.Application` and `Event.Infrastructure` (for
  DI composition only — controllers call into `Application`, never directly
  into `Infrastructure`).
- Both `Event.sln` and root `EventPlanner.sln` get the two new projects added
  under the existing `Event` solution folder.

## Domain Model

`Event` (aggregate root, `Event.Domain`):

| Field | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Assigned inside `Event.CreateNew(...)`, not DB-generated |
| `Name` | `string` | Required, non-empty (throws on empty/whitespace) |
| `Description` | `string?` | Optional; max 255 characters (throws if exceeded) |
| `Location` | `Address` | Value object: `Street`, `City`, `PostalCode`, `Country` (all `string`, required) |
| `StartDate` | `DateTime` (UTC) | — |
| `EndDate` | `DateTime` (UTC) | Must be strictly after `StartDate` (throws otherwise) |

Construction is only via `Event.CreateNew(name, description, location, startDate, endDate)`
— no public parameterless constructor, no public setters. Invalid input throws
(e.g. `ArgumentException`/a domain-specific exception type — see Open Questions).

`IEventRepository` (output port, `Event.Domain`):
```csharp
public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Event @event, CancellationToken ct);
}
```
(Exact method set may grow during Tasks/Implement as use cases are defined —
this is the minimum for "create and read" success criteria.)

## Code Style

Matches existing repo conventions: file-scoped namespaces, `sealed` where
applicable, primary constructors / expression-bodied members where they read
cleanly (see existing `EventItem` record as the current style baseline).
Example of the target `Event` shape:

```csharp
namespace Event.Domain;

public sealed class Event
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public Address Location { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private Event(Guid id, string name, string? description, Address location, DateTime startDate, DateTime endDate)
    {
        Id = id;
        Name = name;
        Description = description;
        Location = location;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Event CreateNew(string name, string? description, Address location, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (description is { Length: > 255 })
            throw new ArgumentException("Description must be 255 characters or fewer.", nameof(description));
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));

        return new Event(Guid.NewGuid(), name, description, location, startDate, endDate);
    }
}
```

## Testing Strategy

- **Framework:** xUnit (repo has no test projects yet — this establishes the
  first one and the convention for `Agenda`/`Bff` to follow later).
- **`Event.Domain.Tests`:** unit tests for `Event.CreateNew` invariants (empty
  name, description > 255 chars, `EndDate` <= `StartDate`, happy path) and the
  `Address` value object.
- **`Event.Infrastructure.Tests`:** integration tests for the `EventRepository`
  adapter and EF mapping against a real PostgreSQL instance — see Open
  Questions for how that instance is provisioned in CI/local dev.
- No test project planned for `Event.Application` in this pass unless
  use-case logic grows beyond trivial pass-through to the repository.

## Boundaries

- **Always:** enforce domain invariants only inside `Event.Domain` (never in
  `Application`, `Infrastructure`, or `Api`); keep `Application` free of any
  reference to `Infrastructure` or EF Core types; run `dotnet build
  Event/Event.sln` and `dotnet test Event/Event.sln` before considering a task
  done.
- **Ask first:** adding new NuGet dependencies beyond
  `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`,
  and `Testcontainers.PostgreSql` (e.g. FluentValidation, MediatR); changing
  `Event.sln`/`EventPlanner.sln` project layout beyond what's specified here;
  any change to `Agenda` or `Bff`.
- **Never:** commit a real database connection string/secret; auto-apply
  migrations against a non-local database without explicit confirmation;
  remove/weaken a domain invariant to make a test pass.

## Success Criteria

- `Event.Domain` compiles with no dependencies on other Event projects and
  cannot construct an invalid `Event`.
- `Event.Infrastructure` has an EF Core migration that creates the events
  table in PostgreSQL, mapping the EF entity (not the domain entity) and the
  `Address` value object as an owned type.
- `Event.Api` exposes working create/get-by-id endpoints backed by real
  Postgres persistence (verified manually via `Event.Api.http` or Scalar UI,
  and/or the integration tests above).
- `dotnet build EventPlanner.sln` and `dotnet build Event/Event.sln` both
  succeed with the new projects wired in.

## Resolved Decisions

1. **Local/dev Postgres:** a root-level `docker-compose.yml` with a single
   `postgres` service. Root-scoped (not `Event/`-scoped) so `Agenda`/`Bff` can
   reuse it later without duplicating the compose file.
2. **Integration test provisioning:** `Testcontainers.PostgreSql` is added as
   a new test-only dependency for `Event.Infrastructure.Tests`, spinning up a
   throwaway Postgres container per test run.
3. **Migration application strategy:** manual only. `Event.Api` never calls
   `dbContext.Database.Migrate()` on startup. Migrations are applied via
   `dotnet ef database update` (locally) or an explicit deploy step (later,
   when wired into `deploy-production.yml` — not part of this pass).
4. **Exception type:** `Event.CreateNew` throws plain `ArgumentException` on
   invalid input — no new custom exception type introduced.

## Open Questions

None outstanding.
