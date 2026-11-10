# Execution report

> **Later local verification (10 November 2026):** Docker startup, SQL procedure order tests, browser flows, and the synthetic SQL benchmark were subsequently run on Windows. See [docs/LOCAL_VERIFICATION.md](docs/LOCAL_VERIFICATION.md) and the updated [README](README.md). The statuses below are the original **7 November authoring-environment** report, not the latest local status.

Date: 7 November 2026. .NET SDK 10.0.403 on Linux.

| Check | Status | Evidence |
|---|---|---|
| Restore, Release build and Razor compilation | PASSED | Final dotnet test built both projects without reported warnings |
| xUnit | PASSED | 14 passed, 0 failed, 0 skipped; docs/test-output.txt |
| SQL Server 2022 syntax parsing | PASSED | All five SQL files; docs/sql-syntax-output.txt |
| JavaScript syntax | PASSED | node --check src/StorefrontCMS/wwwroot/js/cart.js |
| Bash syntax | PASSED | bash -n scripts/setup.sh scripts/sql.sh database/init.sh |
| Compose YAML parsing | PASSED | Python PyYAML parsed db, init and app services; not Docker validation |
| Docker build/Compose startup | NOT EXECUTED HERE | No Docker executable/daemon available |
| SQL order procedure execution | NOT EXECUTED HERE | No SQL Server available |
| SQL benchmark before/after | NOT EXECUTED HERE | No SQL Server available; no timings claimed |
| Browser flow/security/concurrency checks | NOT EXECUTED HERE | Application cannot start without SQL Server |
| PowerShell script execution | NOT EXECUTED HERE | PowerShell not available; intended for PowerShell 7 |

Exact final test command, with the locally installed SDK on PATH:

```sh
dotnet test StorefrontCMS.slnx -c Release -m:1 -p:UseSharedCompilation=false --nologo
```

The initial parallel MSBuild invocation failed because its named-pipe socket creation was denied. Single-process MSBuild with compiler-server sharing disabled completed successfully. Initial restore flagged two advisories against transitive System.Security.Cryptography.Pkcs 9.0.4; an explicit 10.0.14 reference removed those warnings in the final restore. This is not a claim that the application has received a full security audit.

Reproduce the SQL parser check:

```sh
dotnet run --project tools/SqlSyntaxCheck -- database
```

The parser uses Microsoft's ScriptDom TSql160Parser. It checks grammar, not stored-procedure execution, schema binding, query plans, transaction semantics or concurrency. It caught an invalid combined table-variable declaration, which was corrected before the final check.

To execute the missing SQL checks after starting Compose:

```powershell
pwsh -File scripts/sql.ps1 order-tests
pwsh -File scripts/sql.ps1 benchmark | Tee-Object benchmark-results.txt
```

The benchmark requirement is not fully met until real before/after results are captured and inserted into README. A source review, unit test or syntax parse cannot substitute for those measurements.
