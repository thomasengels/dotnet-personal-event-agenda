# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Personal Event Planner — a .NET microservice sample composed of three independent bounded contexts (Event, Agenda, BFF). The codebase is currently a thin scaffold: controllers return hardcoded/stub data, there is no persistence layer, no test projects, and cross-service wiring (e.g. `Bff.Domain.Services.IEventClient`) is an empty stub interface — the BFF does not yet actually call Event or Agenda. Don't assume behavior beyond what's in the code; extend the scaffold rather than treating it as a mature service.

## Commands

Build/run/test use the standard `dotnet` CLI. There is no top-level Makefile or npm scripts.

```bash
# Build everything (root solution references all three service solutions)
dotnet build EventPlanner.sln

# Build/run a single service (can also be done from its own *.sln)
dotnet run --project Event/Event.Api/Event.Api.csproj
dotnet run --project Agenda/Agenda.Api/Agenda.Api.csproj
dotnet run --project Bff/Bff.Api/Bff.Api.csproj

# Equivalent convenience scripts (same as dotnet run above)
./launch-event-api.sh
./launch-agenda-api.sh
./launch-bff-api.sh

# Or use the VS Code tasks (.vscode/tasks.json): "Run Event API", "Run Agenda API",
# "Run BFF API" (auto-starts Event + Agenda first), "Run All APIs"
```

There are currently **no test projects** in the repository — `dotnet test` has nothing to run against any `*.sln`.

Once running, each API serves OpenAPI JSON at `/openapi/v1.json` and a Scalar reference UI at `/scalar/v1`.

## Architecture

Three bounded contexts, each fully isolated in its own solution, each containing exactly one ASP.NET Core Web API project (`*.Api`) and one domain class library (`*.Domain`):

```
Event/   Event.sln    -> Event.Api  (/api/events)     + Event.Domain
Agenda/  Agenda.sln   -> Agenda.Api (/api/agenda)      + Agenda.Domain
Bff/     Bff.sln      -> Bff.Api    (/api/dashboard/{userId}) + Bff.Domain
```

`EventPlanner.sln` at the repo root references all six projects for whole-repo builds/IDE use; the three per-service `*.sln` files are what CI builds and what each service is deployed from independently. There is no shared/common project — each `*.Api` only project-references its own `*.Domain`. The BFF is intended to aggregate Event + Agenda data (see `Bff.Domain.Services.IEventClient`), but that integration isn't implemented yet.

Each `Program.cs` follows the same minimal template: `AddControllers()` (controllers auto-discovered via `[ApiController]`), `AddOpenApi()`, `MapOpenApi()`, `MapScalarApiReference()`, `UseHttpsRedirection()`, `MapControllers()`. Keep new services/changes consistent with this pattern unless there's a reason to diverge.

All projects target `net10.0` with `Nullable` and `ImplicitUsings` enabled.

See `.claude/ARCHITECTURE.md` for the hexagonal-architecture layering rules, including the use case convention: a use case is a sealed class with exactly one public method, `ExecuteAsync`, injected directly into controllers (no use-case-specific interface).

### Deployment

Each API has its own `Dockerfile` and deploys independently to Azure Container Apps via GitHub Actions. Service entry workflows (`deploy-production.yml`, `deploy-selected-branch-development.yml`) call a shared reusable workflow (`build-and-deploy-dotnet-webapp.yml`) with service-specific inputs, running three jobs in sequence: `ci` (restore/build/test) -> `build_and_push` (login to GHCR with the workflow's own `GITHUB_TOKEN`, image built and pushed to `ghcr.io/<owner>/<image_name>`) -> `deploy` (OIDC login to Azure, point the Container App's registry credentials at GHCR via the `GHCR_PAT` secret, deploy the `build_and_push` job's image output to Container Apps). Registry auth uses GHCR (`GITHUB_TOKEN` for push, a long-lived `GHCR_PAT` for the Container App's pull credential); Azure OIDC federation (`azure/login@v3`, no stored client secrets) is only used to manage the Container App resource itself. See `architecture.md` for the full identity/RBAC setup and the ACR->GHCR migration notes, and `azure-roadmap.md` for known OIDC federated-credential issues (e.g. production environment subject mismatches) and their fixes.

## Auto-commit workflow

After every change, commit it and push to the remote without waiting for explicit confirmation each time. Write a clear commit message.

After pushing, launch a subagent to follow the resulting GitHub Actions run to completion. If the triggered run fails, have that subagent capture the failing job/step output and launch a bug-fixing subagent (with that failure output) to diagnose and fix the issue.

## Repo-local skill

`.claude/skills/no-mistakes/SKILL.md` (also mirrored at `.agents/skills/no-mistakes/`) defines a `no-mistakes` validation pipeline (review, test, lint, docs, push, PR, CI) driven via the `no-mistakes axi` CLI. Use it when asked to validate, gate, or safely ship changes in this repo.
