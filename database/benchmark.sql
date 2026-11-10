USE StorefrontCMS;
GO
SET NOCOUNT ON;
-- Isolated table: does not pollute the 200-product catalog. Never run on production.
IF OBJECT_ID('dbo.SearchBenchmark') IS NOT NULL DROP TABLE dbo.SearchBenchmark;
CREATE TABLE dbo.SearchBenchmark(Id int NOT NULL PRIMARY KEY,CategoryId int NOT NULL,Name nvarchar(120) NOT NULL,Price decimal(12,2) NOT NULL,Padding char(400) NOT NULL);
;WITH n AS (SELECT TOP(200000) ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) i FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT dbo.SearchBenchmark SELECT i,1+i%100,CONCAT('Product ',RIGHT('000000'+CONVERT(varchar(6),i),6)),100,'x' FROM n;
DECLARE @Before bigint,@After bigint,@Start datetime2,@i int=0,@Sink decimal(20,2);
-- Identical, sargable search workload. The deliberately missing covering index
-- makes the before phase scan wide rows. Warm cache and 30 repetitions per phase.
SELECT @Sink=SUM(Price) FROM dbo.SearchBenchmark WHERE CategoryId=42 AND Name LIKE 'Product 1%';
SET STATISTICS IO ON;SET STATISTICS TIME ON;
SET @Start=SYSUTCDATETIME();
WHILE @i<30 BEGIN
 SELECT @Sink=SUM(Price) FROM dbo.SearchBenchmark WHERE CategoryId=42 AND Name LIKE 'Product 1%';
 SET @i+=1;
END;
SET @Before=DATEDIFF_BIG(microsecond,@Start,SYSUTCDATETIME());
CREATE INDEX IX_SearchBenchmark_Category_Name ON dbo.SearchBenchmark(CategoryId,Name) INCLUDE(Price);
SELECT @Sink=SUM(Price) FROM dbo.SearchBenchmark WHERE CategoryId=42 AND Name LIKE 'Product 1%';
SET @i=0;SET @Start=SYSUTCDATETIME();
WHILE @i<30 BEGIN
 SELECT @Sink=SUM(Price) FROM dbo.SearchBenchmark WHERE CategoryId=42 AND Name LIKE 'Product 1%';
 SET @i+=1;
END;
SET @After=DATEDIFF_BIG(microsecond,@Start,SYSUTCDATETIME());
SET STATISTICS IO OFF;SET STATISTICS TIME OFF;
SELECT @@VERSION AS SqlVersion,COUNT(*) AS RowsInFixture FROM dbo.SearchBenchmark;
SELECT CAST(@Before/30000.0 AS decimal(18,3)) AS BeforeMeanMs,CAST(@After/30000.0 AS decimal(18,3)) AS AfterMeanMs,30 AS RepetitionsPerPhase,@Sink AS ResultChecksum;
DROP TABLE dbo.SearchBenchmark;
GO
