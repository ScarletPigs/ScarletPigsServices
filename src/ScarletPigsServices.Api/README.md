# Piglet API

The backend for the Piglet Discord bot and frontend.

## Authentication

Users, password hashes, roles, lockout state, and refresh-token metadata are stored
in PostgreSQL through ASP.NET Core Identity and Entity Framework Core.

The API exposes these endpoints:

- `POST /auth/register` with `{ "email": "...", "password": "..." }`
- `POST /auth/login` with `{ "email": "...", "password": "..." }`
- `POST /auth/refresh` with `{ "refreshToken": "..." }`
- `POST /auth/revoke` with a bearer access token and
  `{ "refreshToken": "..." }`

Access tokens are signed JWTs with a default lifetime of 15 minutes. Refresh tokens
default to 30 days, are rotated on use, and are stored only as SHA-256 hashes.
Reusing a rotated refresh token revokes the user's active refresh tokens.

Set `Authentication__SigningKey` to a secret containing at least 32 random bytes.
The AppHost maps its secret `JWT_SIGNING_KEY` parameter to this setting. Issuer,
audience, and token lifetimes can be overridden using the other settings in the
`Authentication` configuration section.

The `UnitOrganizer` and `MissionMaker` roles are created by the Identity migration.
Either role satisfies the `CanUploadMissions` policy.
