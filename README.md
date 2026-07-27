# ScarletPigsServices

ScarletPigsServices is an Aspire distributed application containing the services
used by the Scarlet Pigs community. The application topology is defined in
[`Aspire/ScarletPigsServices.AppHost/AppHost.cs`](Aspire/ScarletPigsServices.AppHost/AppHost.cs).

## Production environment

Production is deployed to Dokploy and currently consists of:

- PostgreSQL, backed by the persistent `scarletpigs-postgres-data` volume.
- An Entity Framework Core migration bundle that runs once before the API starts.
- The externally exposed Scarlet Pigs API.
- OCAP2 Web for mission recording uploads and playback.
- Piglet, the Python Discord bot, which consumes the API.

The AppHost publishes container images to the existing hosted registry and
deploys them through the vendored Dokploy integration. Images are resolved to
immutable registry digests before rollout, and the deployment verifies that the
new Dokploy tasks are running the expected digest.

### Dokploy resource publishing

External resources configure public domains through the same options callback
pattern used by Aspire extension methods:

```csharp
.PublishToDokploy(dokploy, options => options
    .WithDomain("http", "api.scarletpigs.com")
    .WithDomain("https", "api.scarletpigs.com"));
```

`WithDomain` maps a named external Aspire endpoint to a host name without a
scheme or path. Call it more than once to expose additional endpoint and domain
combinations. When no mappings are configured, the integration continues to ask
Dokploy to generate domains automatically. Existing Dokploy domains are updated
when their configured endpoint, host, port, or HTTPS setting changes.

Resources published with `PublishToDokploy` use the readable `latest` image tag
by default. After pushing, the integration resolves that tag to its immutable
registry digest and gives Dokploy the combined `image:latest@sha256:...`
reference. This keeps the readable alias while preventing Docker Swarm from
reusing stale bytes cached under a mutable tag. The pipeline then waits for a
new Swarm task and verifies that its service specification references that exact
digest and remains running through a stability window, or completes successfully
for a one-shot service.

Migration bundles are run-once Dokploy applications. The integration uses a
no-restart, stop-first Swarm policy so a successful bundle can complete without
being restarted or rolled back as though it were a long-running service.
Multiline environment values are encoded for Dokploy's dotenv parser, including
PEM private keys.

For how Dokploy works and how it manages applications, deployments, domains,
environment variables, registries, volumes, and Docker Swarm settings, see the
official [Dokploy documentation](https://docs.dokploy.com/docs/core) and
[applications guide](https://docs.dokploy.com/docs/core/applications).

### API authentication and data contract

Every API controller requires the shared API key in the `X-API-Key` request
header. Configure a random value of at least 32 bytes through the `API_KEY`
deployment secret. The AppHost supplies the same secret to the API and Piglet;
the previous JWT issuance, Identity persistence, and user endpoints are removed.

The API data model is a fresh baseline matching the frontend database contract.
It exposes 17 resources under `/api`, uses `snake_case` JSON fields, and stores
its two enums as native PostgreSQL enum types. Existing databases must be
recreated before deploying this migration history. See the
[API documentation](src/ScarletPigsServices.Api/README.md) for route conventions.

Piglet uses a typed Python client for this contract. On its first launch it
imports the legacy Google Sheets data, then persists a completion marker in
`/api/app-settings`; subsequent launches use only the API for schedule and bot
state.

During local AppHost execution, Aspire runs `dotnet ef database update` and holds
the API until migrations complete. During publishing, Aspire builds a Linux
migration-bundle container that Dokploy deploys using the database connection
supplied by the AppHost.

### OCAP2 Web

OCAP2 Web is exposed at `aar.scarletpigs.com` over HTTP and HTTPS. Aspire uses
the upstream `ghcr.io/ocap2/web:2.1.1` image directly and configures request
logging, recording conversion, and live streaming.

Recordings, maps, and the SQLite database use separate persistent Dokploy
volumes. During local development, those paths are instead bind-mounted from
[`volumes/ocap`](volumes/ocap). Local volume contents are not included in the
published image or copied to Dokploy; populate the production volumes manually
when needed. Production requires the `OCAP_SECRET` and
`OCAP_AUTH_ADMINSTEAMIDS` values.

### Deployment configuration

The GitHub Actions
[`deploy.yml`](.github/workflows/deploy.yml) workflow deploys every push to
`main`. It reads configuration from the `production` GitHub environment,
validates the required values, and runs the Aspire deployment pipeline with the
`Production` environment.

Any parameter added to the Aspire AppHost must also be added to the repository's
`production` GitHub environment as a secret or variable, then mapped and
validated in [`deploy.yml`](.github/workflows/deploy.yml). A parameter change is
not ready for production until the AppHost and repository environment are in
sync.

The website projects are deprecated. They are disabled in the AppHost, are not
part of the production deployment, and should be removed from the repository
before too long.

## Run locally

Install these prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Docker-compatible container runtime
- Python 3.13
- [Aspire CLI](https://aspire.dev/get-started/install-cli/)

The repository currently uses Aspire 13.4.6. If you manage the CLI as a .NET
global tool, install the matching version:

```powershell
dotnet tool install --global Aspire.Cli --version 13.4.6
```

Confirm that the `aspire` command is available on `PATH`:

```powershell
aspire --version
```

From the repository root, start the application with:

```powershell
aspire start
```

The root [`aspire.config.json`](aspire.config.json) points the CLI to the
AppHost, so no AppHost argument is required. In a Git worktree, or when another
instance may already be running, use isolated mode:

```powershell
aspire start --isolated
```

Aspire builds the projects, starts PostgreSQL, applies the database migrations,
starts the API, and prints the dashboard URL. Piglet uses explicit start
behavior; start it from the Aspire dashboard when its required credentials are
configured.

The AppHost reads environment-specific values from the configuration keys named
in `AppHost.cs`. Supply those values through environment variables or store
sensitive local values with the Aspire CLI:

```powershell
aspire secret set <key> <value>
```

## Wiring an Aspire environment

Resources, dependencies, environment variables, startup ordering, and deployment
targets are wired in `AppHost.cs`. Use the Aspire CLI to search the documentation
while changing that model:

```powershell
aspire docs search "environments"
aspire docs get environments
```

For the underlying concepts, see Aspire's
[resource overview](https://aspire.dev/get-started/resources/),
[integration overview](https://aspire.dev/integrations/overview/), and
[deployment guide](https://aspire.dev/deployment/deploy-with-aspire/).

## Deploy

Inspect the deployment pipeline without changing external state:

```powershell
aspire deploy --apphost Aspire/ScarletPigsServices.AppHost/ScarletPigsServices.AppHost.csproj --list-steps
```

Generate deployment artifacts:

```powershell
aspire publish --apphost Aspire/ScarletPigsServices.AppHost/ScarletPigsServices.AppHost.csproj
```

Run the production deployment pipeline:

```powershell
aspire deploy --apphost Aspire/ScarletPigsServices.AppHost/ScarletPigsServices.AppHost.csproj --environment Production
```
