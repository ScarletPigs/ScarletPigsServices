# ScarletPigsServices

Repository hosting the various services used on the Scarlet Pigs server.

## Dokploy deployment

The AppHost includes a vendored Dokploy publishing integration and publishes
PostgreSQL, an EF Core migration bundle, the API, and the Piglet bot. The two website
resources remain disabled and are not included in deployment. The API's HTTP
endpoints are external, so the integration creates or updates its Dokploy domains.
The API configures its public domain through the same options callback pattern
used by Aspire extension methods:

```csharp
.PublishToDokploy(dokploy, options => options
    .WithDomain("http", "api.scarletpigs.com")
    .WithDomain("https", "api.scarletpigs.com"));
```

`WithDomain` maps a named external Aspire endpoint to a host name without a
scheme or path. Call it more than once to expose additional endpoint and domain
combinations. When no mappings are configured, the integration continues to
ask Dokploy to generate domains automatically.

Every API controller requires the shared API key in the `X-API-Key` request
header. Configure a random value of at least 32 bytes through the `API_KEY`
deployment secret. The AppHost supplies the same secret to the API and Piglet;
the previous JWT issuance, Identity persistence, and user endpoints are
removed.

The API data model is a fresh baseline matching the frontend database contract.
It exposes the 17 resources under `/api`, uses `snake_case` JSON fields, and
stores its two enums as native PostgreSQL enum types. Existing databases must
be recreated before deploying this migration history. See the
[API documentation](src/ScarletPigsServices.Api/README.md) for the route
conventions.

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
exact digest and remains running through a stability window (or completes
successfully for a one-shot service). A stale or immediately crashing rollout
therefore fails the pipeline instead of being reported as successfully
deployed. Multiline environment values are encoded for Dokploy's dotenv parser,
including PEM private keys.

Migration bundles are marked as run-once Dokploy applications. The integration
uses a no-restart, stop-first Swarm policy so a successful bundle can complete
without being restarted or rolled back as though it were a long-running service.

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
