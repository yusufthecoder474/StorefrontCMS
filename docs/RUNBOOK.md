# StorefrontCMS support runbook

Run diagnostics against your local StorefrontCMS database in SSMS, or use sqlcmd inside the db container. Never paste passwords or complete connection strings into tickets. Times are UTC. HTTP responses include `X-Correlation-ID`; JSON logs include the same ID in a scope. Browser network tools show the response header. `docker compose logs --since 15m app` shows recent structured logs.

## 1. Checkout reports insufficient stock

**Symptoms:** checkout stays on its form with “Stock changed or a product is unavailable.” Logs have `Checkout completed with Code 1`. This is an expected business rejection, not an unhandled exception. A timeout or SQL deadlock instead produces HTTP 500 and a full exception log.

Find the user's product IDs and correlation ID, then inspect current stock and recent orders:

```sql
USE StorefrontCMS;
DECLARE @ProductId int=1;
SELECT Id,Name,Active,Stock,Price,Version FROM dbo.Products WHERE Id=@ProductId;
SELECT TOP(20) o.Id,o.CreatedUtc,l.Quantity,l.UnitPrice
FROM dbo.OrderLines l JOIN dbo.Orders o ON o.Id=l.OrderId
WHERE l.ProductId=@ProductId ORDER BY o.CreatedUtc DESC;
SELECT TOP(50) Event,Entity,CorrelationId,CreatedUtc
FROM dbo.AuditLog WHERE Event='OrderPlaced' ORDER BY Id DESC;
SELECT session_id,blocking_session_id,wait_type,wait_time,wait_resource
FROM sys.dm_exec_requests WHERE database_id=DB_ID();
```

**Fix:** explain that a cart is not a stock reservation. Remove the inactive product or reduce its quantity; replenish only if real inventory exists. The demo admin edit form can change stock and checks its version. If an edit conflicts, reload first. Never delete order rows or disable stock checks to make checkout pass. For deadlocks, inspect the exception number (1205) and the competing query; retain consistent product lock order. Retrying the same successful request ID cannot create another order. Automatic deadlock retries are not implemented.

**Verify:** place a valid order; check a single order, corresponding lines and audit row, and the exact stock decrement. A failed order must not decrement any line's stock. Run `scripts/sql.ps1 order-tests` on a disposable local database.

## 2. Published banner looks stale

**Symptoms:** admin shows a newly published version but the homepage shows older text. Check that the editor published `homepage-banner`, not another static page. Saving a draft does not publish it.

```sql
USE StorefrontCMS;
SELECT b.Slug,b.PublishedVersionId,v.Title,v.Body,v.CreatedUtc
FROM dbo.CmsBlocks b LEFT JOIN dbo.CmsVersions v ON v.Id=b.PublishedVersionId
WHERE b.Slug IN ('homepage-banner','promo-strip');
SELECT TOP(20) * FROM dbo.CmsPublications WHERE Slug='homepage-banner' ORDER BY Id DESC;
SELECT TOP(20) Event,UserId,Entity,CorrelationId,CreatedUtc
FROM dbo.AuditLog WHERE Entity='homepage-banner' ORDER BY Id DESC;
```

**Fix:** publish the intended version through admin. Successful publish/rollback removes the local memory-cache entry while a shared process gate prevents an in-flight stale reader from refilling it. Direct SQL writes bypass invalidation; wait two minutes or restart the app after confirming the DB pointer. Multiple app replicas are unsupported: use one replica, or add distributed cache invalidation before scaling. Check that the browser points to the same environment/database as admin. Do not log CMS body content unnecessarily.

**Verify:** publish a visibly different title, request `/`, then rollback and request `/` again. SQL pointer, displayed text and audit log should agree. A bad publish must leave the live pointer unchanged.

## 3. Product search becomes slow

**Symptoms:** request logs show elevated `ElapsedMs` for `/Catalog`; database health still returns healthy. Reproduce with the same category, term and page. A deep OFFSET page and a leading-wildcard substring search can both scan more rows.

```sql
USE StorefrontCMS;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
EXEC dbo.ProductSearch @Q=N'Product 1',@CategoryId=2,@Page=1;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
SELECT i.name,i.is_disabled,s.user_seeks,s.user_scans,s.last_user_seek
FROM sys.indexes i LEFT JOIN sys.dm_db_index_usage_stats s
 ON s.object_id=i.object_id AND s.index_id=i.index_id AND s.database_id=DB_ID()
WHERE i.object_id=OBJECT_ID('dbo.Products');
SELECT TOP(10) qs.execution_count,qs.total_elapsed_time/NULLIF(qs.execution_count,0) AS MeanMicroseconds,
 qs.total_logical_reads/NULLIF(qs.execution_count,0) AS MeanLogicalReads,t.text
FROM sys.dm_exec_query_stats qs CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t
WHERE t.dbid=DB_ID() ORDER BY MeanMicroseconds DESC;
```

SQL Server 2022 DMV queries may require `VIEW SERVER PERFORMANCE STATE`; application users should not receive that privilege. Use an authorized diagnostic account.

**Fix:** confirm `IX_Products_Category_Name` exists and is enabled; restore it from schema if missing. Update statistics after substantial data changes: `UPDATE STATISTICS dbo.Products;`. Inspect the actual plan in SSMS. The index helps category filtering and ordering; it cannot turn `%substring%` into a Name seek. At larger scale, consider full-text search or explicitly changing requirements to prefix search, and keyset pagination for deep pages. Do not silently change search semantics.

**Verify:** run the isolated `benchmark.sql` script to compare the same prefix/category workload before and after a covering index. It uses 200,000 synthetic rows, 30 warm repetitions per phase, reports mean milliseconds and logical reads, and drops its fixture. It is an index demonstration, not an end-to-end catalog latency claim. Never flush the shared production buffer cache for a benchmark.
