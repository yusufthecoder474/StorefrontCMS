#!/usr/bin/env bash
set -euo pipefail
case "${1:-order-tests}" in order-tests|benchmark) script="${1:-order-tests}";; *) echo 'Expected order-tests or benchmark'; exit 1;; esac
docker compose exec -T db bash -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -i "$1"' -- "/scripts/$script.sql"
