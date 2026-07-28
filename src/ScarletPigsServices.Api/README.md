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

## OCAP read-through API

Authenticated clients can read OCAP resources through
`GET /api/ocap/{ocap-path}`. The API streams OCAP's status, headers, and body
without buffering large recording files. Supported upstream paths are:

- `/api/healthcheck`, `/api/version`, and `/api/v1/customize`;
- `/api/v1/operations` and its read-only child routes;
- `/api/v1/worlds`;
- `/data/*` recording manifests, chunks, and legacy JSON recordings;
- `/images/*` maps, markers, fonts, and sprites.

For example, `GET /api/ocap/api/v1/operations` returns OCAP's recording
catalogue. Authentication, upload, administration, and live-stream endpoints
are deliberately not proxied.

The AppHost supplies OCAP service discovery to the API. Outside the AppHost,
configure the `http://ocap` service-discovery endpoint and set
`Ocap:PublicBaseUrl` to the browser-facing OCAP base URL.

## Automatic event AAR linking

The API checks events whose `type_key` is `operation` when their `starts_at`
time arrives. It retries once per hour for five hours. Each completed protobuf
recording's manifest supplies its actual UTC start time; the catalogue's
mission duration supplies its end time. The recording with the greatest
overlap with the event timeslot is linked at:

```text
{Ocap:PublicBaseUrl}/recording/{recording-id}/{filename}
```

The resulting URL is stored in the event's `aar_url`. Attempts stop immediately
after a match. The last-attempt timestamp is persisted but excluded from the
JSON contract, allowing retries to retain their cadence across API restarts.

## Database lifecycle

The migration history is intentionally a new baseline and requires a fresh
database. During local AppHost execution, Aspire runs
`dotnet ef database update` and holds the API until the migration completes.
Production publishing creates the same migration as a one-shot migration
bundle.
