# Local verification evidence — 10 November 2026

This document summarizes results provided in browser captures and PowerShell logs from a **Windows 11 / Docker Desktop / SQL Server 2022** run. This is not a claim that the README editor independently executed the application.

## Observed walkthrough

| Check | Evidence | Result |
|---|---|---|
| Startup | `docker compose ps -a`: app Up, db Up (healthy), init Exited (0) | Pass |
| Product seed | Database init logged `(200 rows affected)`; storefront showed 200 products | Pass |
| Checkout | Product 001, one unit, mock order #1 for ₹106.25 | Pass |
| Order history | Customer Orders page displayed order #1 | Pass |
| Stock | Product 001 went from 11 to 10 units | Pass |
| SQL order tests | Printed `PASS: success, price, stock, audit, idempotency, rollback, invalid quantity, empty cart` | Pass |
| CMS | Banner version 4 published; homepage showed the new text | Pass |
| CMS rollback | Homepage returned to `Small store. Clear choices.` | Pass |
| Product admin | Wireless Keyboard created at ₹1,500.00, 15 stock, editor values ₹1,299.00 and 20; deactivation yielded `0 products found` for `wireless` | Browser-confirmed creation/deactivation; edit form input shown |
| Synthetic benchmark | 200,000 rows; 30 repetitions per phase; mean 46.509 ms before and 18.823 ms after | Completed |

**Interpretation:** 59.53% mean-time reduction and 2.47× faster in the isolated indexed prefix/category query. The original site's leading-wildcard substring search is a different workload and was not benchmarked here.

The original [execution report](../EXECUTION_REPORT.md) records 14 passing xUnit tests and five SQL syntax checks in the initial build environment. It predates the Windows runtime walkthrough. No authenticated multi-user isolation test, browser automation suite, concurrent SQL stress test or deployed production run was provided.

## Screenshots

All images in [`screenshots/`](screenshots/) are real user-shared screenshots (cropped to application content when possible). `10-benchmark.png` records the displayed benchmark output. Secrets and `.env` values are intentionally not included.

## Reproduce

```powershell
cd "D:\Downloads\StorefrontCMS-Run\StorefrontCMS"
docker compose ps -a
pwsh -File scripts/sql.ps1 order-tests
pwsh -File scripts/sql.ps1 benchmark | Tee-Object benchmark-results.txt
```

The path is the original operator's extraction location. For another machine, run from its own project directory. Keep the raw benchmark output locally; `benchmark-results.txt` is excluded from Git.
