# Development volumes

This directory contains host folders bind-mounted into container resources
during local Aspire runs. Its contents are not included in published images or
copied to deployment environments.

OCAP uses:

- `ocap/data` for mission recordings
- `ocap/maps` for map tiles
- `ocap/db` for its SQLite database

Dokploy uses separate named volumes for these paths. Populate those production
volumes manually when required.
