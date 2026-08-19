# Todo: Retrieve a user's agenda for a day/week/month

See `plan.md` for architecture decisions and rationale.

## Task 1: `AgendaTimeframe` + `AgendaWindow` domain logic

**Description:** Add the pure calendar-math domain rule that turns a reference date + a
`Day`/`Week`/`Month` selector into a `[Start, End)` `DateTime` window. This is the one piece of
new business logic in this feature, so it gets its own domain type and its own test project,
built and verified before anything depends on it.

**Acceptance criteria:**
- [x] `AgendaTimeframe` enum (`Day`, `Week`, `Month`) exists in `Bff.Domain.Models`
- [x] `AgendaWindow` (record/value type) exposes a factory that computes `(DateTime Start, DateTime End)` from a reference `DateTime` and an `AgendaTimeframe`, with `End` exclusive
- [x] Week and month boundaries are correct across a year boundary (e.g. week spanning Dec 29 – Jan 4; month of December)

**Verification:**
- [x] Tests pass: `dotnet test Bff/Bff.Domain.Tests` (new project, see Files)
- [x] Build succeeds: `dotnet build Bff/Bff.sln`
- [x] Manual check: none (pure unit-testable logic)

**Dependencies:** None

**Files likely touched:**
- `Bff/Bff.Domain/Models/AgendaTimeframe.cs` (new)
- `Bff/Bff.Domain/Models/AgendaWindow.cs` (new)
- `Bff/Bff.Domain.Tests/Bff.Domain.Tests.csproj` (new — mirror `Agenda.Domain.Tests.csproj`)
- `Bff/Bff.Domain.Tests/AgendaWindowTests.cs` (new)
- `Bff/Bff.sln` (add `Bff.Domain.Tests` via `dotnet sln add`)

**Estimated scope:** Small (1-2 files + 1 new test project)

---

## Task 2: `AgendaEntry` model + extend `IAgendaClient`/`IEventClient` ports

**Description:** Add the domain-level shape of one "entry" in a user's agenda view (an agenda
item joined with its event's details), and grow the two existing outbound ports with the read
methods the new use case needs. No implementations yet — just the contracts the rest of the
feature is built against.

**Acceptance criteria:**
- [x] `AgendaEntry` record in `Bff.Domain.Models` combines an agenda item identity with its `EventSummary`
- [x] `IAgendaClient.GetAgendaAsync(int userId, CancellationToken ct)` returns `Task<IReadOnlyList<AgendaItemSummary>>`
- [x] `IEventClient.GetEventsAsync(DateTime startDate, DateTime endDate, CancellationToken ct)` returns `Task<IReadOnlyList<EventSummary>>`
- [x] Existing methods on both interfaces are untouched (no breaking signature changes)

**Verification:**
- [x] Build succeeds: `dotnet build Bff/Bff.sln` (will fail until Task 3 implements the new interface members — acceptable to land Task 2+3 together if preferred, see note below)
- [x] Manual check: none

**Dependencies:** None (can run in parallel with Task 1)

**Files likely touched:**
- `Bff/Bff.Domain/Models/AgendaEntry.cs` (new)
- `Bff/Bff.Domain/Services/IAgendaClient.cs` (edit)
- `Bff/Bff.Domain/Services/IEventClient.cs` (edit)

**Estimated scope:** Small (3 files)

**Note:** Adding a method to an interface breaks every implementer until it's filled in. If
building strictly incrementally, fold Task 2 and Task 3 into one commit; they're listed separately
here only because they're conceptually distinct (port contract vs adapter implementation).

---

## Checkpoint: Domain foundations (after Tasks 1–2)
- [x] `dotnet build Bff/Bff.sln` succeeds (once Task 3's adapters exist too, per the note above)
- [x] `dotnet test Bff/Bff.Domain.Tests` passes
- [x] Review with user before proceeding to orchestration

---

## Task 3: Implement new port methods in the API-layer adapters

**Description:** Implement the two new interface methods against the real downstream HTTP APIs:
`AgendaClient.GetAgendaAsync` calls `GET /api/agenda/{userId}` (Agenda.Api), and
`EventClient.GetEventsAsync` calls `GET /api/events?startDate=&endDate=` (Event.Api). Both
endpoints already exist server-side — this is pure adapter wiring, following the existing
try/catch → `DownstreamServiceUnavailableException` pattern already used by
`AddEventToAgendaAsync`/`GetEventByIdAsync`.

**Acceptance criteria:**
- [x] `AgendaClient.GetAgendaAsync` GETs `/api/agenda/{userId}`, deserializes to `List<AgendaItemSummary>`, wraps `HttpRequestException`/timeout in `DownstreamServiceUnavailableException("Agenda", ...)`
- [x] `EventClient.GetEventsAsync` GETs `/api/events` with `startDate`/`endDate` query params (round-trip `"o"` format), deserializes to `List<EventSummary>`, same exception wrapping with `"Event"`
- [x] Both return an empty list rather than throwing when the downstream returns an empty JSON array

**Verification:**
- [x] Build succeeds: `dotnet build Bff/Bff.sln`
- [x] Manual check: with Agenda.Api and Event.Api running locally, hit each client method via a scratch call (or defer to Task 5's end-to-end check) and confirm real data round-trips

**Dependencies:** Task 2 (port signatures)

**Files likely touched:**
- `Bff/Bff.Api/Services/AgendaClient.cs` (edit)
- `Bff/Bff.Api/Services/EventClient.cs` (edit)

**Estimated scope:** Small (2 files)

---

## Task 4: `Bff.Application` project + `GetUserAgendaUseCase`

**Description:** Create the BFF's first `Application` layer (it doesn't exist yet — today
`DashboardController` calls the clients directly). Add `GetUserAgendaUseCase`: a sealed class with
one `ExecuteAsync(int userId, DateTime? referenceDate, AgendaTimeframe timeframe, CancellationToken ct)`
that resolves the reference date (via `TimeProvider` when omitted), computes the `AgendaWindow`,
fetches the user's agenda items, short-circuits to `[]` if there are none (skipping the Event
call), otherwise fetches events in the window and joins/sorts by `StartDate`. Add the matching
`BffApplicationServiceCollectionExtensions.AddBffApplication()`, mirroring
`AgendaApplicationServiceCollectionExtensions`.

**Acceptance criteria:**
- [x] `GetUserAgendaUseCase` has exactly one public method, `ExecuteAsync`, per ARCHITECTURE.md rule 2
- [x] Returns `AgendaEntry` items sorted ascending by the event's `StartDate`
- [x] Agenda items whose `EventId` has no matching event in the window are excluded (no join match)
- [x] Empty agenda for the user → `IEventClient.GetEventsAsync` is never called (verified via a spy/mock in tests)
- [x] `AddBffApplication()` registers `TimeProvider.System` and `GetUserAgendaUseCase`

**Verification:**
- [x] Tests pass: `dotnet test Bff/Bff.Application.Tests` (new project, mirror `Agenda.Application.Tests` — stub `IAgendaClient`/`IEventClient`, `FakeTimeProvider` pattern already used in `AddEventToAgendaUseCaseTests`)
- [x] Build succeeds: `dotnet build Bff/Bff.sln`
- [x] Manual check: none yet (wired end-to-end in Task 5)

**Dependencies:** Task 1 (`AgendaWindow`), Task 2 (ports/models)

**Files likely touched:**
- `Bff/Bff.Application/Bff.Application.csproj` (new — mirror `Agenda.Application.csproj`)
- `Bff/Bff.Application/UseCases/GetUserAgendaUseCase.cs` (new)
- `Bff/Bff.Application/BffApplicationServiceCollectionExtensions.cs` (new)
- `Bff/Bff.Application.Tests/Bff.Application.Tests.csproj` (new)
- `Bff/Bff.Application.Tests/GetUserAgendaUseCaseTests.cs` (new)
- `Bff/Bff.sln` (add both new projects via `dotnet sln add`)

**Estimated scope:** Medium (4-5 files across 2 new projects)

---

## Checkpoint: Orchestration (after Tasks 3–4)
- [x] `dotnet test` passes for `Bff.Application.Tests` and `Bff.Domain.Tests`
- [x] `dotnet build Bff/Bff.sln` succeeds
- [x] Review with user before wiring the public endpoint

---

## Task 5: Wire the endpoint end-to-end

**Description:** Expose the use case over HTTP: a new `GET` action on `DashboardController`, its
response contract, DI registration in `Program.cs`, and the remaining project/solution wiring
(`Bff.Api` → `Bff.Application` reference, both new test projects and `Bff.Application` added to
`Bff.sln` and root `EventPlanner.sln`).

**Acceptance criteria:**
- [x] `GET /api/dashboard/{userId}/agenda?date=&timeframe=` (timeframe: `Day`/`Week`/`Month`, default `Day`; date optional, defaults to now) returns the joined agenda entries as JSON
- [x] `userId <= 0` → 400, matching the existing validation style in `DashboardController`/`AgendaController`
- [x] `DownstreamServiceUnavailableException` → 503, matching the existing catch block in `AddSelectedEventToAgenda`
- [x] `Bff.Api.csproj` references `Bff.Application`; `Program.cs` calls `AddBffApplication()`
- [x] `Bff.sln` and `EventPlanner.sln` both include `Bff.Application`, `Bff.Application.Tests`, `Bff.Domain.Tests`

**Verification:**
- [x] Build succeeds: `dotnet build EventPlanner.sln`
- [x] Tests pass: `dotnet test` at repo root (or per new project)
- [x] Manual check: start Event.Api, Agenda.Api, Bff.Api locally (`./launch-*.sh` or VS Code "Run All APIs"); `POST` an event onto a user's agenda; call the new endpoint with a day/week/month window covering and then excluding that event's `StartDate`; confirm inclusion, exclusion, and sort order
- [x] Manual check: existing `POST /api/dashboard/{userId}/agenda/events/{eventId}` and `GET /api/agenda/{userId}` (Agenda.Api) flows still behave as before (no regression)

**Dependencies:** Task 3 (adapters), Task 4 (use case)

**Files likely touched:**
- `Bff/Bff.Api/Contracts/AgendaEntryResponse.cs` (new)
- `Bff/Bff.Api/Controllers/DashboardController.cs` (edit)
- `Bff/Bff.Api/Program.cs` (edit)
- `Bff/Bff.Api/Bff.Api.csproj` (edit — add `Bff.Application` reference)
- `Bff/Bff.sln`, `EventPlanner.sln` (edit)

**Estimated scope:** Medium (5 files)

---

## Checkpoint: Complete
- [x] All acceptance criteria across Tasks 1–5 met
- [x] `dotnet build EventPlanner.sln` and full test suite green
- [x] Manual end-to-end check performed and passing
- [x] Ready for review
