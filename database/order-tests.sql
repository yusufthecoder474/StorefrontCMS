USE StorefrontCMS;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Disposable local database only. Dedicated fixtures avoid altering seed products.
DECLARE @Code int,@User int,@Product int,@Other int,@Category int=(SELECT MIN(Id) FROM dbo.Categories),@Key uniqueidentifier=NEWID(),@Correlation varchar(64)='sql-test-'+CONVERT(varchar(36),NEWID());
INSERT dbo.Users(Username,PasswordHash,Role) VALUES(CONCAT('test-',NEWID()),'not-a-login-hash','Customer'); SET @User=SCOPE_IDENTITY();
INSERT dbo.Products(CategoryId,Name,Description,Price,Stock) VALUES(@Category,'Test A','Test fixture',10,5); SET @Product=SCOPE_IDENTITY();
INSERT dbo.Products(CategoryId,Name,Description,Price,Stock) VALUES(@Category,'Test B','Test fixture',20,1); SET @Other=SCOPE_IDENTITY();
DECLARE @Items dbo.CartTable;
DECLARE @Result TABLE(Code int,OrderId int);
BEGIN TRY
 INSERT @Items VALUES(@Product,2);
 INSERT @Result EXEC dbo.PlaceOrder @User,@Key,@Items,@Correlation;
 IF NOT EXISTS(SELECT 1 FROM @Result WHERE Code=0 AND OrderId IS NOT NULL) THROW 52000,'Success result failed',1;
 IF (SELECT Stock FROM dbo.Products WHERE Id=@Product)<>3 THROW 52000,'Stock decrement failed',1;
 IF (SELECT Total FROM dbo.Orders WHERE UserId=@User)<>20 THROW 52000,'Server price failed',1;
 IF (SELECT COUNT(*) FROM dbo.AuditLog WHERE UserId=@User)<>1 THROW 52000,'Order audit failed',1;
 DELETE @Result;
 INSERT @Result EXEC dbo.PlaceOrder @User,@Key,@Items,@Correlation;
 IF (SELECT COUNT(*) FROM dbo.Orders WHERE UserId=@User)<>1 OR (SELECT Stock FROM dbo.Products WHERE Id=@Product)<>3 THROW 52000,'Idempotent retry failed',1;
 DELETE @Items; INSERT @Items VALUES(@Product,1),(@Other,2); SET @Key=NEWID();
 -- A procedure ROLLBACK cannot execute inside INSERT EXEC. Capture its result
 -- directly and assert state separately for rejected orders.
 EXEC @Code=dbo.PlaceOrder @User,@Key,@Items,@Correlation;
 IF @Code<>1 THROW 52000,'Expected insufficient-stock code 1',1;
 IF (SELECT Stock FROM dbo.Products WHERE Id=@Product)<>3 OR (SELECT Stock FROM dbo.Products WHERE Id=@Other)<>1 THROW 52000,'Insufficient-stock rollback failed',1;
 IF (SELECT COUNT(*) FROM dbo.Orders WHERE UserId=@User)<>1 OR (SELECT COUNT(*) FROM dbo.AuditLog WHERE UserId=@User)<>1 THROW 52000,'Rejected order left data',1;
 DELETE @Items; INSERT @Items VALUES(@Product,0); SET @Key=NEWID();
 EXEC @Code=dbo.PlaceOrder @User,@Key,@Items,@Correlation;
 IF @Code<>2 THROW 52000,'Expected invalid-cart code 2',1;
 IF EXISTS(SELECT 1 FROM dbo.Orders WHERE UserId=@User AND RequestId=@Key) THROW 52000,'Invalid quantity accepted',1;
 DELETE @Items; SET @Key=NEWID(); EXEC @Code=dbo.PlaceOrder @User,@Key,@Items,@Correlation;
 IF @Code<>2 THROW 52000,'Expected invalid-cart code 2',1;
 IF EXISTS(SELECT 1 FROM dbo.Orders WHERE UserId=@User AND RequestId=@Key) THROW 52000,'Empty cart accepted',1;
 PRINT 'PASS: success, price, stock, audit, idempotency, rollback, invalid quantity, empty cart';
END TRY
BEGIN CATCH
 DELETE dbo.AuditLog WHERE UserId=@User;
 DELETE l FROM dbo.OrderLines l JOIN dbo.Orders o ON o.Id=l.OrderId WHERE o.UserId=@User;
 DELETE dbo.Orders WHERE UserId=@User;DELETE dbo.Products WHERE Id IN (@Product,@Other);DELETE dbo.Users WHERE Id=@User;
 THROW;
END CATCH;
DELETE dbo.AuditLog WHERE UserId=@User;
DELETE l FROM dbo.OrderLines l JOIN dbo.Orders o ON o.Id=l.OrderId WHERE o.UserId=@User;
DELETE dbo.Orders WHERE UserId=@User;DELETE dbo.Products WHERE Id IN (@Product,@Other);DELETE dbo.Users WHERE Id=@User;
GO
