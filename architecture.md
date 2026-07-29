# Personal Event Planner CI/CD Architecture

## Purpose
This document describes the current working GitHub Actions deployment architecture for Agenda, BFF, and Event APIs, including the Azure identity and authorization setup required for successful runs.

It only includes changes that made real progress and are still part of the active setup.

## High-Level Flow
1. A service-specific workflow starts on push to master for its own folder, or via manual dispatch.
2. The service-specific workflow calls one shared reusable workflow.
3. The shared workflow runs three jobs in sequence: ci, build_and_push, deploy.
4. The build_and_push job logs into Azure with OIDC, reads ACR credentials from Key Vault, builds and pushes image to ACR.
5. The deploy job logs into Azure with OIDC and deploys the new image to Azure Container Apps.

## Workflow Topology
Service workflows:
1. .github/workflows/build-agenda.yml
2. .github/workflows/build-bff.yml
3. .github/workflows/build-events.yml

Shared reusable workflow:
1. .github/workflows/build-and-deploy-dotnet-webapp.yml

Each service workflow passes service-specific values:
1. dockerfile
2. build_context
3. dotnet_test_path
4. image_name
5. registry_login_server
6. acr_name
7. container_app_name
8. resource_group
9. key_vault_name
10. environment_name

Each service workflow passes shared Azure secrets into the reusable workflow:
1. AZURE_CLIENT_ID
2. AZURE_TENANT_ID
3. AZURE_SUBSCRIPTION_ID

## Shared Workflow Jobs
### ci
1. Checkout source.
2. Setup .NET SDK.
3. Restore dependencies.
4. Build in Release mode.
5. Test in Release mode.
6. Upload test result artifacts.

### build_and_push
1. Login to Azure using azure/login with client-id, tenant-id, subscription-id.
2. Read registry username and password from Key Vault secrets.
3. Login to ACR using retrieved credentials.
4. Build Docker image.
5. Push two tags:
1. SHA tag
2. latest tag

### deploy
1. Login to Azure using azure/login.
2. Deploy to Azure Container Apps with azure/container-apps-deploy-action.
3. Deploy image is passed deterministically as `${{ inputs.registry_login_server }}/${{ inputs.image_name }}:${{ github.sha }}`.
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
2. Key Vault Secrets User on vault:
1. scope: /subscriptions/058933dd-7e7a-4224-bebe-92cd54f1a97c/resourceGroups/event-agenda-project/providers/Microsoft.KeyVault/vaults/event-agenda-kv

Why both are needed:
1. Contributor enables deployment operations on target resources.
2. Key Vault Secrets User enables Microsoft.KeyVault/vaults/secrets/getSecret/action for registry secret retrieval.

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
6. Key Vault Secrets User role was assigned so Key Vault secret reads succeed.
7. Deploy image wiring was stabilized by using a deterministic image reference in deploy instead of relying on cross-job output propagation.

## Verification Status
Latest validation commit:
1. c82b8f7b5b2693c65c32974dd9387b4bdd8dc857

Verified successful workflow runs on that commit:
1. Build and deploy EventServiceApi to Azure Container Apps (run 30460001817)
2. Build and deploy BFF to Azure Container Apps (run 30460001702)
3. Build and deploy Agenda to Azure Container Apps (run 30460008906)

## Operational Notes
1. If Azure login fails with No matching federated identity, check the exact subject shown in the error and compare to Entra federated credential subjects.
2. If Key Vault secret retrieval fails with ForbiddenByRbac, validate Key Vault Secrets User assignment at vault scope.
3. If changes were just made in Entra or RBAC, allow a short propagation window before rerunning.
