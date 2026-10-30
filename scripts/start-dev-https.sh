#!/usr/bin/env bash
set -euo pipefail
set +x

cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."

umask 077
mkdir -p .https
read -r -s -p 'Choose a local certificate password: ' certificate_password
printf '\n'
trap 'unset certificate_password' EXIT
if [[ -z "$certificate_password" ]]; then
    printf 'Certificate password must not be empty.\n' >&2
    exit 1
fi

dotnet dev-certs https --export-path .https/hotdesk.pfx --password "$certificate_password"
dotnet dev-certs https --trust
dotnet user-secrets set 'Kestrel:Certificates:Default:Password' "$certificate_password" --project src/Web.Api
unset certificate_password

docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.https.yml up --build -d
