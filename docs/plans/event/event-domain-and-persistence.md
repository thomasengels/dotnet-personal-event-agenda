# Event Domain & Persistence — Design Intent

Confirmed intent, captured via interview before any spec/plan/code work starts.

## Outcome

A rich `Event` domain entity with real PostgreSQL persistence, replacing the
current stub controller data, built as Ports & Adapters (Hexagonal
Architecture) across four sibling projects in the `Event` bounded context:
`Event.Api`, `Event.Application`, `Event.Domain`, `Event.Infrastructure`.

## Why now

Moving `Event` from hardcoded stub data (per `CLAUDE.md`: "controllers return
hardcoded/stub data, there is no persistence layer") to a real, validated
domain model with real persistence.

## Domain shape (`Event.Domain`)

`Event` entity:

| Field | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Assigned inside a `CreateNew(...)` factory method — not DB-generated |
| `Name` | `string` | Required, non-empty |
| `Description` | `string?` | Optional, max 255 chars |
| `Location` | `Address` (value object) | `Street`, `City`, `PostalCode`, `Country` |
| `StartDate` | `DateTime` (UTC) | — |
| `EndDate` | `DateTime` (UTC) | Must be strictly after `StartDate` |

All invariants above are enforced by the domain entity itself (in the
`CreateNew` factory / constructor), not by the API layer, Application layer,
or database constraints.

`Event.Domain` also defines the output port interface (e.g. `IEventRepository`)
that `Application` depends on.

## Architecture (Ports & Adapters)

- **`Event.Domain`** — `Event` entity, `Address` value object, output port
  interface(s) (e.g. `IEventRepository`). No dependency on any other project.
- **`Event.Application`** — use-case logic. Depends on the port interfaces
  defined in `Domain`. Does **not** reference `Event.Infrastructure`.
- **`Event.Infrastructure`** — EF Core `DbContext`, a separate EF-mapped
  `Event` persistence entity (distinct from the domain `Event` entity), an
  outbound adapter implementing `IEventRepository` (maps EF entity ↔ domain
  entity), EF Core migrations targeting PostgreSQL.
- **`Event.Api`** — wires DI (binds `IEventRepository` to the Infrastructure
  adapter), calls into `Application`.

This follows the repo's existing per-bounded-context isolation (see
`CLAUDE.md`): each context's projects are self-contained, no shared project
across `Event` / `Agenda` / `Bff`.

## Success criteria

`Event.Api` can create/read `Event`s backed by PostgreSQL via EF Core
migrations. Invalid `Event`s (bad date range, empty name, description over
255 chars) are rejected by the domain itself.

## Constraints

- Event-bounded-context only — this work does not touch `Agenda` or `Bff`.
- No shared/common project introduced across bounded contexts.
- `Application` must depend only on the port interface, never directly on
  `Infrastructure`.

## Out of scope

- `Agenda` and `Bff` persistence.
- Multi-timezone/localization beyond UTC-normalized timestamps.
- Geocoding/coordinates on `Address`.
- Auth/authz.

## Assumptions folded in without an explicit round-trip

These were reasonable defaults filled in during the interview rather than
directly confirmed — flag/correct during implementation if wrong:

- `Name` has no explicit max length.
- `Description` is optional (nullable), not required.
- The date invariant is `EndDate` strictly **after** `StartDate` (not
  "on or after").

## Next steps

This document is the confirmed intent. Downstream: `spec-driven-development`
to turn this into an implementable spec, then `planning-and-task-breakdown`.
