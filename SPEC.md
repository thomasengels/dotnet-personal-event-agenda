# Spec: Abstract Base Entity for Id-Driven Domain Entities

## Objective

Follow the newly added architectural rule (`.claude/ARCHITECTURE.md`, rule
13): id-driven domain entities inherit from a common abstract entity base
that owns identity and identity-based equality, instead of each entity
hand-rolling its own `Id` property and equality members.

Introduce `Entity<TId>` in `Event.Domain` and migrate `Event` (the only
id-driven entity in this bounded context) onto it. `Address` is a value
object (no identity) and is unaffected.

Scoped to the `Event` bounded context only. `Agenda.Domain.Models.AgendaItem`
is also id-driven but is a `record` — converting it to a class deriving from
an abstract base is a larger, separate change and is flagged below as a
follow-up rather than bundled into this spec.

## Tech Stack

Matches the rest of the repo — no new dependencies:

- .NET 10 (`net10.0`), C#, `Nullable` + `ImplicitUsings` enabled
- xUnit for domain tests

## Commands

```bash
dotnet build Event/Event.sln
dotnet test Event/Event.sln
```

## Project Structure

```
Event/Event.Domain/Entity.cs        -> new abstract Entity<TId> base
Event/Event.Domain/Event.cs         -> inherits Entity<Guid>, drops its own Id property
Event/Event.Domain.Tests/EntityTests.cs -> new (identity-equality contract, via a test double)
Event/Event.Domain.Tests/EventTests.cs  -> extend with equality tests for Event itself
```

No changes to `Event.Infrastructure`, `Event.Application`, or `Event.Api` —
`Id` remains a public get-only property on `Event` with the same type
(`Guid`), so every existing call site (`EventRepository`, `EventResponse`,
`EventsController`, tests) keeps compiling unchanged.

## Code Style

```csharp
// Event.Domain/Entity.cs
namespace Event.Domain;

public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; }

    protected Entity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
```

Equality is by `(runtime type, Id)` — two entities of different derived
types never compare equal even if `Id` matches, and two `Event` instances
with the same `Id` are equal regardless of other property values (identity,
not structural, equality — the point of the rule).

`Event` becomes:

```csharp
// Event.Domain/Event.cs
namespace Event.Domain;

public sealed class Event : Entity<Guid>
{
    public string Name { get; }
    public string? Description { get; }
    public Address Location { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private Event(Guid id, string name, string? description, Address location, DateTime startDate, DateTime endDate)
        : base(id)
    {
        Name = name;
        Description = description;
        Location = location;
        StartDate = startDate;
        EndDate = endDate;
    }

    // CreateNew / Reconstitute unchanged
}
```

## Testing Strategy

- **`Event.Domain.Tests/EntityTests.cs`** (new) — exercises the equality
  contract through a private test-double subclass (e.g. `TestEntity(Guid) :
  Entity<Guid>`), since `Entity<TId>` itself is abstract:
  - two instances with the same `Id` -> equal, same hash code
  - two instances with different `Id`s -> not equal
  - instances of two different `Entity<Guid>` subclasses with the same `Id`
    -> not equal
  - an entity is equal to itself (reference) and never equal to `null`
- **`Event.Domain.Tests/EventTests.cs`** (extend) — two `Event`s built via
  `Reconstitute` with the same `Id` but different `Name`/dates are equal;
  two `Event`s from separate `CreateNew` calls (distinct generated `Id`s)
  are not equal.
- No changes needed to `Event.Application.Tests` or
  `Event.Infrastructure.Tests` — neither depends on `Event` equality
  semantics.

Run via `dotnet test Event/Event.sln`.

## Boundaries

- **Always do:** keep `Entity<TId>` in `Event.Domain` (no shared/common
  project exists per repo convention — each bounded context that needs this
  gets its own copy), keep `Id` public get-only exactly as before so
  existing call sites don't change, add the equality tests before
  considering this done.
- **Ask first:** introducing a shared project to de-duplicate `Entity<TId>`
  across bounded contexts, changing `Event`'s public constructor surface
  beyond what's needed to route through `base(id)`.
- **Never do:** give `Entity<TId>` a public or settable `Id` (identity is
  fixed at construction), make equality structural (comparing all
  properties) instead of identity-based, touch `Agenda.Domain.AgendaItem` in
  this change.

## Success Criteria

- `Event` derives from `Entity<Guid>` and no longer declares its own `Id`
  property.
- `Event.Domain.Tests` passes, including new equality-contract tests for
  both `Entity<TId>` (via test double) and `Event`.
- `dotnet build Event/Event.sln` and `dotnet test Event/Event.sln` succeed
  with no changes required outside `Event.Domain` / `Event.Domain.Tests`.

## Open Questions

- **Assumption, not yet confirmed:** equality is identity-based
  `(runtime type, Id)`, matching standard DDD entity-equality practice.
  Flagged for review.
- **Follow-up, out of scope here:** `Agenda.Domain.Models.AgendaItem` is
  also id-driven (`int Id`) but is currently a `record`. Migrating it onto
  an `Entity<TId>` base (in `Agenda.Domain`, its own copy per the
  no-shared-project convention) means converting it from a record to a
  class, which changes its equality/`with`-expression semantics — a
  separate change, not bundled here.
