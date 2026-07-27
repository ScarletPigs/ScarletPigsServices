# OCAP deployment files

This folder builds a thin Scarlet Pigs image on top of
the immutable multi-platform digest published as `ghcr.io/ocap2/web:2.1.1`
from upstream Git revision `f6069eb43708ac45e24d0446c79ee75e7420cfdf`.

Add repository-managed OCAP files beneath:

- `volume/data` for mission recordings
- `volume/maps` for map tiles
- `volume/db` for SQLite database files

The image synchronizes those files into the corresponding persistent container
volumes each time it starts. A repository file therefore replaces a volume file
at the same relative path on the next deployment, while files created only at
runtime remain persistent.

The AppHost supplies the required `OCAP_SECRET` and
`OCAP_AUTH_ADMINSTEAMIDS` environment variables. Do not store either value in
this folder.
