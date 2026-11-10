# First-run SQL Server fix

This copy adds `-I` (QUOTED_IDENTIFIER ON) to SQLCMD calls in `database/init.sh`, `scripts/sql.ps1`, `scripts/sql.sh`, and the Docker database health check. This is needed for DML against the filtered Products index. No application logic or tests were changed.

**Verification:** static source review only; Docker/SQL Server/.NET execution was not available in this environment. If you previously started the original project and it failed during initialization, `docker compose down -v` removes its disposable SQL volume and all data. Use it only when you are comfortable deleting the demo database.

Windows PowerShell (from project folder):

```powershell
if (-not (Test-Path .env)) { pwsh -File scripts/setup.ps1 }
docker compose up --build -d
docker compose ps -a
docker compose logs init
```
