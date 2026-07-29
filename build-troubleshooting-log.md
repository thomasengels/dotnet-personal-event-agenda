# Build Troubleshooting Log

## Scope
- Repository: dotnet-personal-event-agenda
- Branch: master
- Constraint: only actions within this repository and its GitHub/Azure deployment setup

## Timeline
- 2026-07-29: Started focused troubleshooting iteration for failing GitHub Actions runs after commit 57e1816.

## Actions
1. Collected local baseline:
   - Ran `git status --short` and `git log --oneline -n 8`.
   - Observed only untracked local utility files plus existing workflow commits.
2. Collected latest workflow run summary:
   - Ran `gh run list --limit 10 --json ...`.
   - Observed latest three runs (Agenda/BFF/Event) failed on head SHA `57e1816`.
3. Pulled newest failed run logs:
   - Command: `gh run view 30459034767 --log`.
   - Result: pending analysis in next steps.
4. Investigated latest failing run details (head SHA `57e1816`):
    - Retrieved run/job metadata with `gh run view 30459034767 --json jobs`.
    - Observed:
       - `ci` job: success.
       - `build_and_push` job: success.
       - `deploy` job: failure in step `Deploy Container App revision`.
    - Extracted deploy error from logs:
       - `One of the following arguments must be provided: appSourcePath, imageToDeploy, yamlConfigPath`.
    - In deploy action trace, `containerAppName` and `resourceGroup` were present but `imageToDeploy` path was treated as missing.
5. Applied workflow fix to avoid brittle cross-job output resolution for deploy image:
    - File: `.github/workflows/build-and-deploy-dotnet-webapp.yml`.
    - Changed deploy input:
       - From: `imageToDeploy: ${{ needs.build_and_push.outputs.image }}`
       - To: `imageToDeploy: ${{ inputs.registry_login_server }}/${{ inputs.image_name }}:${{ github.sha }}`
    - Rationale:
       - The image reference is deterministic and equals the image produced in `build_and_push`.
       - This removes dependency on reusable-workflow job output propagation for the deploy action input.
