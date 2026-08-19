# Implementation Plan: Retrieve a user's agenda for a day/week/month

## Overview

Add a use case that answers "what's on my agenda for this day/week/month?" — i.e. filter the
user's agenda entries by when the underlying **event** actually occurs, not by when the item was
added to the agenda.

## Architecture decisions

- **Filter basis: event occurrence date**, not `AgendaItem.CreatedUtc`. "What's on my agenda for
  Tuesday" means events whose `StartDate`/`EndDate` fall in that window. Confirmed with the user.
- **Placement: BFF**, not the Agenda service. `AgendaItem` (Agenda.Domain) only stores
  `UserId`, `EventId`, `CreatedUtc` — it has no event date. That data lives in `Event.Domain.Event`.
  Keeping Agenda's domain free of Event's data avoids coupling/staleness (a denormalization
  alternative was considered and rejected — confirmed with the user). This is exactly the
  aggregation role `Bff.Domain.Services.IEventClient`/`IAgendaClient` already exist for, per
  CLAUDE.md ("BFF is intended to aggregate Event + Agenda data... isn't implemented yet").
- **Window computation is a domain rule**, not orchestration: a new `AgendaTimeframe` enum
  (`Day`/`Week`/`Month`) and `AgendaWindow` value type live in `Bff.Domain.Models` and compute the
  `[Start, End)` range from a reference date. Per `.claude/ARCHITECTURE.md` rule 10/11, this keeps
  the use case a thin orchestrator (load → invoke domain → return) rather than a place where
  calendar-math business rules get buried.
- **No new Event/Agenda API endpoints needed.** `GET /api/events?startDate=&endDate=` already
  exists (`Event.Application.UseCases.GetEventsUseCase`) and `GET /api/agenda/{userId}` already
  exists (`Agenda.Application.UseCases.GetAgendaUseCase`). The BFF use case:
  1. fetches the user's agenda items (event IDs) from Agenda,
  2. fetches events in the computed window from Event,
  3. intersects on event ID, joins, sorts by `StartDate`.
- **This becomes the BFF's first real use case.** Today `DashboardController` calls
  `IEventClient`/`IAgendaClient` directly with no `Bff.Application` layer. This task introduces
  `Bff.Application` (mirroring `Agenda.Application`'s structure: sealed class, single
  `ExecuteAsync`, a `Add*Application()` DI extension) so the orchestration isn't stuck in the
  controller, consistent with ARCHITECTURE.md rule 1–3.
- Two existing ports grow one method each rather than gaining new interfaces:
  `IAgendaClient.GetAgendaAsync(userId, ct)` and `IEventClient.GetEventsAsync(startDate, endDate, ct)`.
- New test projects: `Bff.Domain.Tests` (pure `AgendaWindow` logic — none exists today, and this is
  exactly the kind of pure business rule the repo's other `*.Domain.Tests` projects cover) and
  `Bff.Application.Tests` (use case orchestration with stub clients, mirroring
  `Agenda.Application.Tests`).
- Use `dotnet new`/`dotnet sln add` for new projects rather than hand-editing `.sln` GUIDs.

## Task list

### Phase 1: Domain foundations (parallel-safe, no cross-dependency)
- [ ] Task 1: `AgendaTimeframe` + `AgendaWindow` domain logic (Bff.Domain, new Bff.Domain.Tests)
- [ ] Task 2: `AgendaEntry` model + extend `IAgendaClient`/`IEventClient` ports (Bff.Domain)

### Checkpoint: Domain foundations
- [ ] `dotnet build Bff/Bff.sln` succeeds
- [ ] `dotnet test Bff/Bff.Domain.Tests` passes (Task 1's window tests)

### Phase 2: Adapters and orchestration
- [ ] Task 3: Implement the two new port methods in `Bff.Api/Services` (EventClient, AgendaClient)
- [ ] Task 4: `Bff.Application` project + `GetUserAgendaUseCase` + `Bff.Application.Tests`

### Checkpoint: Orchestration
- [ ] `dotnet test` passes for `Bff.Application.Tests`
- [ ] Use case tests cover: intersection filtering, ordering by `StartDate`, empty-agenda short
      circuit (no `IEventClient` call when the user has no agenda items)

### Phase 3: Wire up the endpoint
- [ ] Task 5: `AgendaEntryResponse` contract, `DashboardController` endpoint, `Program.cs` DI,
      csproj/sln wiring for the two new projects

### Checkpoint: End-to-end
- [ ] `dotnet build EventPlanner.sln` succeeds
- [ ] All tests pass across the repo
- [ ] Manual check: run Event, Agenda, and Bff APIs; add an event to a user's agenda; call the new
      endpoint for day/week/month windows that include and exclude that event; verify inclusion,
      exclusion, and ordering; verify an event outside the window but still on the agenda is absent
- [ ] Manual check: `AddAgendaItemResult`/error paths untouched — regression check on existing
      `POST /api/dashboard/{userId}/agenda/events/{eventId}` flow

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Week/month boundary math (timezones, week-start convention, month-end) | Medium — wrong window silently drops or includes events | Dedicated `Bff.Domain.Tests` for `AgendaWindow` before any orchestration code is written (Task 1 first, fails fast) |
| N+1 growth if agenda has many items | Low today (single window-based Event query, not per-item) | Already avoided by design: one `GetEventsAsync(window)` call, not one per agenda item |
| Unbounded `GetEventsAsync` window vs `GetAgendaAsync` no filter | Low — both lists bounded by a single user's data and a single window | No pagination added; acceptable for current scaffold scale, flag if this becomes real data volume |

## Open questions

- None outstanding — filter basis and placement were resolved with the user before planning.
- Week-start convention (Monday-based ISO week vs Sunday-based) is left to implementation Task 1;
  default to Monday-based unless corrected, and cover both bounds in tests either way.
