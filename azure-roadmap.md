Azure OIDC / Federated Identity - Roadmap

Summary
-------
Deployment to production fails with AADSTS700213 (No matching federated identity record) for subject:

  repo:thomasengels@9317188/dotnet-personal-event-agenda@1312813821:environment:production

The production deploy workflow uses the reusable shared workflow with environment: production (.github/workflows/deploy-production.yml).

Root cause (evidence)
---------------------
- The GitHub Actions OIDC token "sub" value includes owner/repo numeric IDs plus the environment segment.
- Azure AD service principal/federated credential does not contain a matching Subject (or has a different Audience/Issuer), so Azure rejects the presented assertion.

Breakthrough solutions (only those that resolve the issue)
---------------------------------------------------------
1) Quick, recommended fix — Add exact federated credential subject for PROD
   - In Azure Portal: App registrations -> [service principal / app used for deployments] -> Certificates & secrets -> Federated credentials -> Add credential with:
     - Issuer: https://token.actions.githubusercontent.com
     - Subject: repo:thomasengels@9317188/dotnet-personal-event-agenda@1312813821:environment:production
     - Audiences: api://AzureADTokenExchange
   - This accepts the exact OIDC subject presented by GitHub for the production environment.

   CLI example (requires up-to-date Azure CLI extension / permissions):
   - Create a JSON payload file (gh-federated-prod.json):
     {
       "name": "gh-prod-environment",
       "issuer": "https://token.actions.githubusercontent.com",
       "subject": "repo:thomasengels@9317188/dotnet-personal-event-agenda@1312813821:environment:production",
       "audiences": ["api://AzureADTokenExchange"]
     }
   - Then run (replace <appId> with the Azure AD Application (client) ID):
     az rest --method POST --uri "https://graph.microsoft.com/v1.0/applications/<appId>/federatedIdentityCredentials" --body @gh-federated-prod.json

2) Alternative — Make federated credential resilient to subject variants
   - If earlier credentials used a different subject format (no numeric IDs), add a second federated credential covering that subject as well (e.g. repo:thomasengels/dotnet-personal-event-agenda:environment:production).
   - Note: Do not rely on overly-broad wildcards; prefer explicit subjects for security.

3) Alternative (if using custom audience) — ensure audience matches
   - If the federated credential uses a non-default audience, set azure/login's audience input to the configured value:
     uses: azure/login@v3
     with:
       audience: "<custom-audience>"
   - Or update the credential to include api://AzureADTokenExchange.

Verification steps
------------------
- After adding the federated credential, run the production workflow (push to master or trigger) and confirm azure/login step succeeds.
- If still failing, capture the exact id_token sub and aud from the failing job's diagnostics (add a short step to the workflow to request id-token via the actions OIDC provider and echo masked values) and compare to the federated credential.

Notes and next actions
----------------------
- The failing token subject in the error message is authoritative; the quickest path is to add that exact subject to Azure.
- If preferred, provide the Azure AD application/client ID and permission to run the az rest command and this repo's workflows can be re-run to verify.

Files referenced
----------------
- .github/workflows/build-and-deploy-dotnet-webapp.yml (reusable workflow)
- .github/workflows/deploy-production.yml (invokes the shared workflow with environment: production)

If you want, can add the exact az rest command (with appId) and apply it, or prepare a portal checklist to walk a colleague through the fix.