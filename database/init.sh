#!/usr/bin/env bash
set -euo pipefail
sql=/opt/mssql-tools18/bin/sqlcmd
exists=$($sql -S db -U sa -C -I -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('StorefrontCMS') IS NULL THEN 0 ELSE 1 END" -b)
if [[ "$exists" == *1* ]]; then
  echo 'Database exists; leaving data intact. For a clean demo reset, see README.'
else
  for script in schema seed procedures; do
    $sql -S db -U sa -C -I -b -i "/scripts/$script.sql"
  done
fi
