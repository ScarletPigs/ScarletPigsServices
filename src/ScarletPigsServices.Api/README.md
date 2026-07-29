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

Read-only collections written by the addon (see below) are documented under
"Addon"; they are not part of the generic CRUD contract.

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

## Addon

`POST /addon/user-info` is the single call-home endpoint for the in-game addon.
Like every other route it requires the `X-API-Key` header.

```json
{
  "steam_id": "76561198000000000",
  "profile_name": "PlayerName",
  "mission_name": "My operation!",
  "owned_dlc": [11123, 314253, 523423]
}
```

`steam_id` and the `owned_dlc` elements are stored as `bigint` and accept either
JSON numbers or numeric strings. All other properties use the same `snake_case`
contract as the rest of the API.

The endpoint stamps a UTC timestamp on arrival and then:

1. **Always** reconciles `steam_dlc_ownership` so the posted `owned_dlc` list
   becomes the exact truth for that player — missing rows are inserted and rows
   no longer in the list are deleted.
2. Checks whether the timestamp falls inside the operation window, **Sunday
   13:00-19:00 Europe/Stockholm wall-clock time**. Stored timestamps stay UTC;
   only the gate converts, so the window is the same clock reading whether CET
   or CEST is observed. The window is half-open (`>= 13:00`, `< 19:00`).
3. If, and only if, the window is open:
   - appends to `profile_name_history`, but only when `profile_name` differs
     from that player's most recent entry;
   - records one `mission_attendance` row per player, mission and session date,
     so repeated posts during a session do not duplicate. A unique index
     enforces this, making the endpoint idempotent under concurrent posts.

The response reports what was actually stored, which is the quickest way to see
whether the gate was open:

```json
{
  "steam_id": 76561198000000000,
  "received_at": "2026-08-02T12:00:00+00:00",
  "owned_dlc_count": 3,
  "within_session_window": true,
  "session_date": "2026-08-02",
  "profile_name_recorded": true,
  "attendance_recorded": true
}
```

Read access to the three datasets, all paged with `offset` and `limit` on the
same terms as the data API:

- `GET /api/attendance` — newest first.
- `GET /api/attendance/mission?name={missionName}` — exact match. The name is a
  query parameter because mission names contain spaces and punctuation.
- `GET /api/attendance/range?start={iso}&end={iso}` — `start` inclusive, `end`
  exclusive.
- `GET /api/attendance/{steamId}`
- `GET /api/dlc-ownership` — grouped per player:
  `[{ "steam_id": 76561198000000000, "owned_dlc": [11123, 314253] }]`. Paging is
  over players, not rows, so a player's list is never split across pages.
- `GET /api/dlc-ownership/{steamId}` — `404` when the player owns nothing.
- `GET /api/profile-names` — newest first.
- `GET /api/profile-names/{steamId}`

## Database lifecycle

The migration history is intentionally a new baseline and requires a fresh
database. During local AppHost execution, Aspire runs
`dotnet ef database update` and holds the API until the migration completes.
Production publishing creates the same migration as a one-shot migration
bundle.
