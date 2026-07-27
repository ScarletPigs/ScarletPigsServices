#!/bin/sh
set -eu

copy_repository_files() {
    source_path="$1"
    destination_path="$2"

    mkdir -p "$destination_path"
    if [ -d "$source_path" ]; then
        cp -a "$source_path"/. "$destination_path"/
    fi
}

copy_repository_files /opt/scarletpigs-ocap-volume/data /var/lib/ocap/data
copy_repository_files /opt/scarletpigs-ocap-volume/maps /var/lib/ocap/maps
copy_repository_files /opt/scarletpigs-ocap-volume/db /var/lib/ocap/db

exec /entrypoint.sh "$@"
