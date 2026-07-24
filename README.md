# ScarletPigsServices

Repository hosting the various services used on the Scarlet Pigs server.

## Dokploy deployment

The AppHost includes a vendored Dokploy publishing integration and publishes
PostgreSQL, the migration service, the API, and the Piglet bot. The two website
resources remain disabled and are not included in deployment. The API's HTTP
endpoint is external, so the integration creates or updates its Dokploy domain.

The Dokploy environment uses an existing hosted container registry. During
deployment, Aspire prompts for:

- the Dokploy API URL and API key;
- the container registry URL, username, and password;
- any application parameters and secrets that have not already been configured.

Inspect the deployment pipeline without changing external state:

```powershell
aspire deploy --apphost Aspire/ScarletPigsServices.AppHost/ScarletPigsServices.AppHost.csproj --list-steps
```

Generate deployment artifacts:

```powershell
aspire publish --apphost Aspire/ScarletPigsServices.AppHost/ScarletPigsServices.AppHost.csproj
```

Run the full build, push, provisioning, and Dokploy deployment pipeline:

```powershell
aspire deploy --apphost Aspire/ScarletPigsServices.AppHost/ScarletPigsServices.AppHost.csproj
```

PostgreSQL uses the `scarletpigs-postgres-data` volume so database data persists
across application deployments.
