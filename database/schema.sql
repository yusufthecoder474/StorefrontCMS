IF DB_ID('StorefrontCMS') IS NULL CREATE DATABASE StorefrontCMS;
GO
USE StorefrontCMS;
GO
IF OBJECT_ID('dbo.Products') IS NOT NULL THROW 51000, 'Schema already exists; use a fresh database.', 1;
CREATE TABLE dbo.Categories(Id int IDENTITY PRIMARY KEY,Name nvarchar(80) NOT NULL UNIQUE);
CREATE TABLE dbo.Products(Id int IDENTITY PRIMARY KEY,CategoryId int NOT NULL REFERENCES dbo.Categories(Id),Name nvarchar(120) NOT NULL,Description nvarchar(2000) NOT NULL,Price decimal(12,2) NOT NULL CHECK(Price>0),Stock int NOT NULL CHECK(Stock>=0),Active bit NOT NULL DEFAULT 1,Version bigint NOT NULL DEFAULT 1);
CREATE INDEX IX_Products_Category_Name ON dbo.Products(CategoryId,Name,Id) INCLUDE(Price,Stock) WHERE Active=1;
CREATE TABLE dbo.Users(Id int IDENTITY PRIMARY KEY,Username nvarchar(80) NOT NULL UNIQUE,PasswordHash nvarchar(500) NOT NULL,Role varchar(10) NOT NULL CHECK(Role IN ('Customer','Admin')));
CREATE TABLE dbo.Orders(Id int IDENTITY PRIMARY KEY,UserId int NOT NULL REFERENCES dbo.Users(Id),RequestId uniqueidentifier NOT NULL,CreatedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),Total decimal(14,2) NOT NULL,CONSTRAINT UQ_Order_Request UNIQUE(UserId,RequestId));
CREATE INDEX IX_Orders_User_Created ON dbo.Orders(UserId,CreatedUtc DESC) INCLUDE(Total);
CREATE TABLE dbo.OrderLines(OrderId int NOT NULL REFERENCES dbo.Orders(Id),ProductId int NOT NULL REFERENCES dbo.Products(Id),Name nvarchar(120) NOT NULL,Quantity int NOT NULL CHECK(Quantity>0),UnitPrice decimal(12,2) NOT NULL,PRIMARY KEY(OrderId,ProductId));
CREATE TABLE dbo.CmsBlocks(Slug varchar(40) PRIMARY KEY,PublishedVersionId int NULL);
CREATE TABLE dbo.CmsVersions(Id int IDENTITY PRIMARY KEY,Slug varchar(40) NOT NULL REFERENCES dbo.CmsBlocks(Slug),Title nvarchar(120) NOT NULL,Body nvarchar(4000) NOT NULL,CreatedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
ALTER TABLE dbo.CmsBlocks ADD CONSTRAINT FK_Cms_Published FOREIGN KEY(PublishedVersionId) REFERENCES dbo.CmsVersions(Id);
CREATE TABLE dbo.CmsPublications(Id int IDENTITY PRIMARY KEY,Slug varchar(40) NOT NULL REFERENCES dbo.CmsBlocks(Slug),VersionId int NOT NULL REFERENCES dbo.CmsVersions(Id),CreatedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE INDEX IX_CmsPublications_Slug_Id ON dbo.CmsPublications(Slug,Id DESC) INCLUDE(VersionId);
CREATE TABLE dbo.AuditLog(Id bigint IDENTITY PRIMARY KEY,Event varchar(40) NOT NULL,UserId int NOT NULL REFERENCES dbo.Users(Id),Entity nvarchar(80) NOT NULL,CorrelationId varchar(64) NOT NULL,CreatedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TYPE dbo.CartTable AS TABLE(ProductId int PRIMARY KEY,Quantity int NOT NULL);
GO
