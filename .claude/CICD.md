# Personal Event Planner CI/CD Architecture

## Purpose
This document describes the current working GitHub Actions deployment architecture for Agenda, BFF, and Event APIs, including the Azure identity and authorization setup required for successful runs.

It only includes changes that made real progress and are still part of the active setup.

## High-Level Flow
1. A service-specific workflow starts on push to master for its own folder, or via manual dispatch.
2. The service-specific workflow calls one shared reusable workflow.
3. The shared workflow runs three jobs in sequence: ci, build_and_push, deploy.
4. The build_and_push job logs into GitHub Container Registry (GHCR) with the workflow's own `GITHUB_TOKEN`, builds the image, and pushes it to `ghcr.io/<owner>/<image_name>`.
5. The deploy job logs into Azure with OIDC, points the Container App's registry credentials at GHCR using a long-lived PAT, and deploys the new image to Azure Container Apps.

## Workflow Topology
Service workflows:
1. .github/workflows/deploy-production.yml (push to master; builds/deploys agenda, bff, events)
2. .github/workflows/deploy-selected-branch-development.yml (manual dispatch; pick branch/SHA and service)

Shared reusable workflow:
1. .github/workflows/build-and-deploy-dotnet-webapp.yml

Each service workflow passes service-specific values:
1. dockerfile
2. build_context
3. dotnet_test_path
4. image_name
5. container_app_name
6. resource_group
7. environment_name

The image is always published to `ghcr.io/<repository_owner>/<image_name>`; there is no per-service registry input anymore.

Each service workflow passes secrets into the reusable workflow:
1. AZURE_CLIENT_ID
2. AZURE_TENANT_ID
3. AZURE_SUBSCRIPTION_ID
4. GHCR_PAT (classic PAT, `read:packages` scope — the long-lived credential Azure Container Apps uses to pull from GHCR)

## Shared Workflow Jobs
### ci
1. Checkout source.
2. Setup .NET SDK.
3. Restore dependencies.
4. Build in Release mode.
5. Test in Release mode.
6. Upload test result artifacts.

### build_and_push
1. Login to `ghcr.io` using `docker/login-action` with the workflow's ephemeral `GITHUB_TOKEN` (`packages: write` permission).
2. Build Docker image.
3. Push tags to `ghcr.io/<owner>/<image_name>`:
1. SHA tag (always)
2. latest tag (only when `publish_latest: true`, currently production only)

### deploy
1. Login to Azure using azure/login.
2. Run `az containerapp registry set` to point the Container App's registry credentials at `ghcr.io`, using `github.actor` as username and the `GHCR_PAT` secret as password. `GITHUB_TOKEN` cannot be used here because it expires when the workflow run ends, and Container Apps needs a durable credential to pull the image on later restarts/scale-outs.
3. Deploy to Azure Container Apps with azure/container-apps-deploy-action, using the exact image reference produced by the `build_and_push` job (`needs.build_and_push.outputs.image`).
4. Concurrency lock prevents overlapping deployments per app and environment.

## Identity and Security Architecture
### GitHub to Azure Authentication
Authentication is workload identity federation through OIDC. No client secret is used by the workflow.

Required GitHub workflow permissions:
1. id-token: write
2. contents: read

This is present in:
1. Reusable workflow permissions.
2. Service workflow permissions.
3. Deploy job permissions in reusable workflow.

### Federated Credentials in Entra App
App registration in use:
1. appId: 8477369f-7c73-4b15-86a9-f63a1f85a21e

Current active federated credentials:
1. github-master
1. subject: repo:thomasengels@9317188/dotnet-personal-event-agenda@1312813821:ref:refs/heads/master
2. github-development-env
1. subject: repo:thomasengels@9317188/dotnet-personal-event-agenda@1312813821:environment:development

Issuer and audience used by both credentials:
1. issuer: https://token.actions.githubusercontent.com
2. audience: api://AzureADTokenExchange

Reason this format matters:
1. Subject matching is exact and case-sensitive.
2. This repository emits the subject format with owner and repository IDs, so plain repo format does not match.

### Azure RBAC Required by Pipeline
Role assignments currently required and present:
1. Contributor on resource group:
1. scope: /subscriptions/058933dd-7e7a-4224-bebe-92cd54f1a97c/resourceGroups/event-agenda-project

Why it's needed:
1. Contributor enables deployment operations on target resources, including `az containerapp registry set` and revision creation.

Key Vault Secrets User is no longer required: registry credentials for push are the workflow's own `GITHUB_TOKEN`, and the pull credential handed to Container Apps is the `GHCR_PAT` GitHub secret, not a Key Vault secret.

### GHCR Registry Credential (GHCR_PAT)
1. Type: GitHub PAT, classic, scope `read:packages`.
2. Stored as a GitHub Actions secret named `GHCR_PAT`, referenced by both `deploy-production.yml` and `deploy-selected-branch-development.yml`.
3. Used only in the `deploy` job, passed to `az containerapp registry set --server ghcr.io --username <github.actor> --password <GHCR_PAT>` so the Container App can pull the image at runtime (on restarts/scale-outs), independent of any single workflow run.
4. GHCR packages remain private; there is no public/anonymous pull path.

## Environment Naming Contract
The current working environment name is lowercase:
1. development

This value must stay aligned across:
1. Workflow input environment_name.
2. GitHub Environment name.
3. Federated credential subject segment environment:development.

## Stabilization Changes That Are Retained
These fixes unblocked failures and are part of the current setup:
1. Reusable workflow secret contract changed from a single azure_credentials secret to three explicit AZURE_* secrets.
2. Caller workflows now pass AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID explicitly.
3. id-token: write permission was added to enable OIDC token issuance.
4. Federated credentials were aligned to the exact presented subject format emitted by GitHub for this repository.
5. environment naming was normalized to lowercase development to match the active GitHub Environment and OIDC subject.
6. Deploy image wiring was stabilized by using the `build_and_push` job's `image` output instead of recomputing the reference in the deploy job.

## Registry Migration: ACR -> GHCR
The image registry moved from Azure Container Registry to GitHub Container Registry. What changed:
1. `build_and_push` no longer logs into Azure or reads Key Vault; it logs into `ghcr.io` with `GITHUB_TOKEN` and pushes there directly.
2. `registry_login_server`, `acr_name`, and `key_vault_name` inputs were removed from the reusable workflow and both callers. Images now always resolve to `ghcr.io/<repository_owner>/<image_name>`.
3. `deploy` gained a `GHCR_PAT` secret and an `az containerapp registry set` step so the Container App has a durable credential to pull from GHCR (see "GHCR Registry Credential" above).
4. One-time manual step outside the workflow: create the `GHCR_PAT` (classic PAT, `read:packages`) and add it as a repository secret before the next run of either deploy workflow.

## Verification Status
Latest validation commit (pre-GHCR-migration, ACR-based):
1. c82b8f7b5b2693c65c32974dd9387b4bdd8dc857

Verified successful workflow runs on that commit (ACR, superseded):
1. Build and deploy EventServiceApi to Azure Container Apps (run 30460001817)
2. Build and deploy BFF to Azure Container Apps (run 30460001702)
3. Build and deploy Agenda to Azure Container Apps (run 30460008906)

The GHCR-based pipeline has not yet had a verified production/development run recorded here — update this section after the first successful run post-migration.

## Operational Notes
1. If Azure login fails with No matching federated identity, check the exact subject shown in the error and compare to Entra federated credential subjects.
2. If `az containerapp registry set` or the subsequent pull fails with an auth error, confirm `GHCR_PAT` is set, unexpired, and still has `read:packages` scope, and that the GHCR package hasn't been switched to a stricter visibility that blocks `github.actor`.
3. If changes were just made in Entra or RBAC, allow a short propagation window before rerunning.
