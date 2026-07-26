# ScarletPigsServices

ScarletPigsServices is an Aspire distributed application containing the services
used by the Scarlet Pigs community. The application topology is defined in
[`Aspire/ScarletPigsServices.AppHost/AppHost.cs`](Aspire/ScarletPigsServices.AppHost/AppHost.cs).

## Production environment

Production is deployed to Dokploy and currently consists of:

- PostgreSQL, backed by the persistent `scarletpigs-postgres-data` volume.
- An Entity Framework Core migration bundle that runs once before the API starts.
- The externally exposed Scarlet Pigs API.
- Piglet, the Python Discord bot, which consumes the API.

The AppHost publishes container images to the existing hosted registry and
deploys them through the vendored Dokploy integration. Images are resolved to
immutable registry digests before rollout, and the deployment verifies that the
new Dokploy tasks are running the expected digest.

For how Dokploy works and how it manages applications, deployments, domains,
environment variables, registries, volumes, and Docker Swarm settings, see the
official [Dokploy documentation](https://docs.dokploy.com/docs/core) and
[applications guide](https://docs.dokploy.com/docs/core/applications).

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
