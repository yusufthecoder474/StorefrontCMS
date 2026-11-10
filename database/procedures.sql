USE StorefrontCMS;
GO
CREATE OR ALTER PROCEDURE dbo.ProductSearch @Q nvarchar(120)=NULL,@CategoryId int=NULL,@Page int=1 AS
BEGIN
 SET NOCOUNT ON;
 IF @Page<1 OR @Page>100000 THROW 51001,'Invalid page.',1;
 -- Escape LIKE metacharacters: search is a literal substring, not a pattern API.
 DECLARE @Pattern nvarchar(500)=N'%'+REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(@Q,N''),N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[')+N'%';
 SELECT * FROM dbo.Products WHERE Active=1 AND (@CategoryId IS NULL OR CategoryId=@CategoryId) AND (@Q IS NULL OR Name LIKE @Pattern ESCAPE '~') ORDER BY Name,Id OFFSET (@Page-1)*12 ROWS FETCH NEXT 12 ROWS ONLY OPTION(RECOMPILE);
 SELECT COUNT(*) FROM dbo.Products WHERE Active=1 AND (@CategoryId IS NULL OR CategoryId=@CategoryId) AND (@Q IS NULL OR Name LIKE @Pattern ESCAPE '~') OPTION(RECOMPILE);
END;
GO
CREATE OR ALTER PROCEDURE dbo.ProductGet @Id int AS
 SELECT * FROM dbo.Products WHERE Id=@Id AND Active=1;
GO
CREATE OR ALTER PROCEDURE dbo.CategoryList AS SELECT Id,Name FROM dbo.Categories ORDER BY Name;
GO
CREATE OR ALTER PROCEDURE dbo.ProductSave @Id int,@CategoryId int,@Name nvarchar(120),@Description nvarchar(2000),@Price decimal(12,2),@Stock int,@Version bigint AS
BEGIN
 SET NOCOUNT ON;
 IF @Id=0 INSERT dbo.Products(CategoryId,Name,Description,Price,Stock) VALUES(@CategoryId,@Name,@Description,@Price,@Stock);
 ELSE BEGIN
 UPDATE dbo.Products SET CategoryId=@CategoryId,Name=@Name,Description=@Description,Price=@Price,Stock=@Stock,Version=Version+1 WHERE Id=@Id AND Active=1 AND Version=@Version;
 IF @@ROWCOUNT=0 THROW 51003,'Product changed; reload before editing.',1;
 END
END;
GO
CREATE OR ALTER PROCEDURE dbo.ProductDelete @Id int,@Version bigint AS
BEGIN
 UPDATE dbo.Products SET Active=0,Version=Version+1 WHERE Id=@Id AND Version=@Version AND Active=1;
 IF @@ROWCOUNT=0 THROW 51003,'Product changed; reload before deleting.',1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.UserGet @Username nvarchar(80) AS SELECT * FROM dbo.Users WHERE Username=@Username;
GO
CREATE OR ALTER PROCEDURE dbo.UserBootstrap @Username nvarchar(80),@PasswordHash nvarchar(500),@Role varchar(10) AS
BEGIN
 SET XACT_ABORT ON;
 BEGIN TRAN;
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WITH(UPDLOCK,HOLDLOCK) WHERE Username=@Username)
 INSERT dbo.Users(Username,PasswordHash,Role) VALUES(@Username,@PasswordHash,@Role);
 COMMIT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.PlaceOrder @UserId int,@RequestId uniqueidentifier,@Items dbo.CartTable READONLY,@CorrelationId varchar(64) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @OrderId int,@ProductId int,@Quantity int,@Stock int,@Price decimal(12,2),@Name nvarchar(120);
 DECLARE @Locked TABLE(ProductId int PRIMARY KEY,Quantity int,Name nvarchar(120),Price decimal(12,2));
 BEGIN TRY
 BEGIN TRAN;
 -- Serializes retries for the same customer/request using the unique index range.
 SELECT @OrderId=Id FROM dbo.Orders WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@UserId AND RequestId=@RequestId;
 IF @OrderId IS NOT NULL BEGIN COMMIT; SELECT 0 Code,@OrderId OrderId; RETURN; END;
 IF NOT EXISTS(SELECT 1 FROM @Items) OR (SELECT COUNT(*) FROM @Items)>50 OR EXISTS(SELECT 1 FROM @Items WHERE Quantity<1 OR Quantity>99)
 BEGIN ROLLBACK; SELECT 2 Code,CAST(NULL AS int) OrderId; RETURN 2; END;
 -- Consistent lock order lowers deadlock risk. Locks are retained through commit.
 DECLARE inventory CURSOR LOCAL FAST_FORWARD FOR SELECT ProductId,Quantity FROM @Items ORDER BY ProductId;
 OPEN inventory; FETCH NEXT FROM inventory INTO @ProductId,@Quantity;
 WHILE @@FETCH_STATUS=0
 BEGIN
 SET @Stock=NULL;
 SELECT @Stock=Stock,@Price=Price,@Name=Name FROM dbo.Products WITH(UPDLOCK,HOLDLOCK,ROWLOCK) WHERE Id=@ProductId AND Active=1;
 IF @Stock IS NULL OR @Stock<@Quantity
 BEGIN CLOSE inventory; DEALLOCATE inventory; ROLLBACK; SELECT 1 Code,CAST(NULL AS int) OrderId; RETURN 1; END;
 INSERT @Locked VALUES(@ProductId,@Quantity,@Name,@Price);
 FETCH NEXT FROM inventory INTO @ProductId,@Quantity;
 END;
 CLOSE inventory; DEALLOCATE inventory;
 INSERT dbo.Orders(UserId,RequestId,Total) SELECT @UserId,@RequestId,SUM(Quantity*Price) FROM @Locked;
 SET @OrderId=SCOPE_IDENTITY();
 INSERT dbo.OrderLines(OrderId,ProductId,Name,Quantity,UnitPrice) SELECT @OrderId,ProductId,Name,Quantity,Price FROM @Locked;
 UPDATE p SET Stock=Stock-l.Quantity,Version=Version+1 FROM dbo.Products p JOIN @Locked l ON p.Id=l.ProductId;
 INSERT dbo.AuditLog(Event,UserId,Entity,CorrelationId) VALUES('OrderPlaced',@UserId,CONVERT(nvarchar(80),@OrderId),@CorrelationId);
 COMMIT; SELECT 0 Code,@OrderId OrderId;
 END TRY
 BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
 END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.OrderHistory @UserId int AS
 SELECT TOP(100) Id,CreatedUtc,Total FROM dbo.Orders WHERE UserId=@UserId ORDER BY CreatedUtc DESC,Id DESC;
GO
CREATE OR ALTER PROCEDURE dbo.OrderDetail @UserId int,@Id int AS
BEGIN
 SELECT Id,CreatedUtc,Total FROM dbo.Orders WHERE Id=@Id AND UserId=@UserId;
 SELECT l.Name,l.Quantity,l.UnitPrice FROM dbo.OrderLines l JOIN dbo.Orders o ON o.Id=l.OrderId WHERE o.Id=@Id AND o.UserId=@UserId ORDER BY l.ProductId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.CmsPublished @Slug varchar(40) AS
 SELECT v.* FROM dbo.CmsBlocks b JOIN dbo.CmsVersions v ON v.Id=b.PublishedVersionId WHERE b.Slug=@Slug;
GO
CREATE OR ALTER PROCEDURE dbo.CmsList AS
BEGIN SELECT * FROM dbo.CmsBlocks ORDER BY Slug; SELECT * FROM dbo.CmsVersions ORDER BY Id DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.CmsDraft @Slug varchar(40),@Title nvarchar(120),@Body nvarchar(4000) AS
BEGIN
 SET XACT_ABORT ON;
 BEGIN TRAN;
 IF NOT EXISTS(SELECT 1 FROM dbo.CmsBlocks WITH(UPDLOCK,HOLDLOCK) WHERE Slug=@Slug) INSERT dbo.CmsBlocks(Slug) VALUES(@Slug);
 INSERT dbo.CmsVersions(Slug,Title,Body) VALUES(@Slug,@Title,@Body);
 COMMIT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.CmsPublish @Slug varchar(40),@VersionId int,@UserId int,@CorrelationId varchar(64) AS
BEGIN
 SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRAN;
 DECLARE @Current int;
 SELECT @Current=PublishedVersionId FROM dbo.CmsBlocks WITH(UPDLOCK,HOLDLOCK) WHERE Slug=@Slug;
 IF NOT EXISTS(SELECT 1 FROM dbo.CmsVersions WHERE Id=@VersionId AND Slug=@Slug) THROW 51004,'Version does not belong to block.',1;
 IF @Current IS NULL OR @Current<>@VersionId
 BEGIN
 UPDATE dbo.CmsBlocks SET PublishedVersionId=@VersionId WHERE Slug=@Slug;
 INSERT dbo.CmsPublications(Slug,VersionId) VALUES(@Slug,@VersionId);
 INSERT dbo.AuditLog(Event,UserId,Entity,CorrelationId) VALUES('CmsPublished',@UserId,@Slug,@CorrelationId);
 END;
 COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.CmsRollback @Slug varchar(40),@UserId int,@CorrelationId varchar(64) AS
BEGIN
 SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRAN;
 DECLARE @Current int,@Previous int;
 SELECT @Current=PublishedVersionId FROM dbo.CmsBlocks WITH(UPDLOCK,HOLDLOCK) WHERE Slug=@Slug;
 SELECT TOP(1) @Previous=VersionId FROM dbo.CmsPublications WHERE Slug=@Slug AND VersionId<>@Current ORDER BY Id DESC;
 IF @Previous IS NULL THROW 51005,'No previous published version.',1;
 UPDATE dbo.CmsBlocks SET PublishedVersionId=@Previous WHERE Slug=@Slug;
 INSERT dbo.CmsPublications(Slug,VersionId) VALUES(@Slug,@Previous);
 INSERT dbo.AuditLog(Event,UserId,Entity,CorrelationId) VALUES('CmsRolledBack',@UserId,@Slug,@CorrelationId);
 COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.HealthCheck AS SELECT 1 FROM dbo.Categories;
GO
