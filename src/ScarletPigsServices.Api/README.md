# Scarlet Pigs API

The backend for the Scarlet Pigs frontends and Piglet Discord bot.

## Authentication

Every endpoint requires the shared API key in the `X-API-Key` request header.
Set `ApiKey__Key` to a random value containing at least 32 bytes. The AppHost
maps its `API_KEY` secret to this setting and supplies the same value to Piglet.

JWT issuance, ASP.NET Core Identity storage, and the former `/auth` and
`/users/me` endpoints are intentionally removed while shared API-key
authentication is in use.

## Data API

The data contract uses the same `snake_case` property names as the generated
frontend database types. PostgreSQL stores the `mod_side` and `override_mode`
enums natively and uses `jsonb` for JSON values.

Collection routes:

- `/api/admin-audit`
- `/api/app-settings`
- `/api/banner-images`
- `/api/capabilities`
- `/api/discord-roles`
- `/api/event-types`
- `/api/events`
- `/api/highlight-videos`
- `/api/mission-uploads`
- `/api/modlists`
- `/api/mods`
- `/api/profiles`
- `/api/role-overrides`
- `/api/user-capability-overrides`

Each collection supports:

- `GET` with optional `offset` and `limit` query parameters;
- `GET /{id}`;
- `POST` with the matching insert shape;
- `PATCH /{id}` with any non-key fields from the matching update shape;
- `DELETE /{id}`.

The composite-key collections use both key values in their item routes:

- `/api/modlist-mods/{modlistId}/{steamId}`
- `/api/role-capabilities/{roleId}/{capabilityKey}`
- `/api/user-discord-roles/{userId}/{roleId}`

Swagger is available at `/swagger` in Development. Select the `ApiKey` security
scheme and enter the configured key to call endpoints from the UI.

## Database lifecycle

The migration history is intentionally a new baseline and requires a fresh
database. During local AppHost execution, Aspire runs
`dotnet ef database update` and holds the API until the migration completes.
Production publishing creates the same migration as a one-shot migration
bundle.
