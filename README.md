# ScarletPigsServices

Repository hosting the various services used on the Scarlet Pigs server.

## Dokploy deployment

The AppHost includes a vendored Dokploy publishing integration and publishes
PostgreSQL, an EF Core migration bundle, the API, and the Piglet bot. The two website
resources remain disabled and are not included in deployment. The API's HTTP
endpoint is external, so the integration creates or updates its Dokploy domain.

During local AppHost execution, Aspire runs `dotnet ef database update` and
holds the API until migrations complete. During publishing, Aspire builds a
Linux migration-bundle container that Dokploy deploys using the database
connection supplied by the AppHost.

The Dokploy environment uses an existing hosted container registry. During
deployment, Aspire prompts for:

- the Dokploy API URL and API key;
- the container registry URL, username, and password;
- any application parameters and secrets that have not already been configured.

Resources published with `PublishToDokploy` use the readable `latest` image tag
by default. After pushing, the integration resolves that tag to its immutable
registry digest and gives Dokploy the combined `image:latest@sha256:...`
reference. This keeps the readable alias while preventing Docker Swarm from
reusing stale bytes cached under a mutable tag. The pipeline then waits for a
new Swarm task and verifies that its service specification references that
exact digest. A stale rollout therefore fails the pipeline instead of being
reported as successfully deployed.

The [Dokploy deployment workflow](.github/workflows/deploy.yml) runs on every
push to `main`. Create a GitHub environment named `production` and populate the
environment secrets referenced by the workflow. The workflow validates every
required value before running `aspire deploy --environment Production`.

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
