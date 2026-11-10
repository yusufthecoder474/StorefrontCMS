param([ValidateSet('order-tests','benchmark')][string]$Script='order-tests')
$ErrorActionPreference='Stop'
docker compose exec -T db bash -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -i "$1"' -- "/scripts/$Script.sql"
if ($LASTEXITCODE -ne 0) { throw "SQL script failed: $Script" }
