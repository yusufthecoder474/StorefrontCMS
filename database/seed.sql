USE StorefrontCMS;
GO
INSERT dbo.Categories(Name) VALUES(N'Electronics'),(N'Home'),(N'Books'),(N'Clothing');
;WITH n AS (SELECT 1 i UNION ALL SELECT i+1 FROM n WHERE i<200)
INSERT dbo.Products(CategoryId,Name,Description,Price,Stock)
SELECT (i-1)%4+1,CONCAT(N'Product ',RIGHT('000'+CAST(i AS varchar(3)),3)),CONCAT(N'Practical demonstration item ',i,N'. Seed data, not a real listing.'),CAST(99+i*7.25 AS decimal(12,2)),10+i%30 FROM n OPTION(MAXRECURSION 200);
INSERT dbo.CmsBlocks(Slug) VALUES('homepage-banner'),('promo-strip'),('about');
INSERT dbo.CmsVersions(Slug,Title,Body) VALUES('homepage-banner',N'Small store. Clear choices.',N'Explore our demonstration catalog and try a mock checkout.'),('promo-strip',N'Practice store',N'No real payments or shipments.'),('about',N'About StorefrontCMS',N'A student-built storefront with transactional inventory and versioned content.');
UPDATE b SET PublishedVersionId=v.Id FROM dbo.CmsBlocks b JOIN dbo.CmsVersions v ON v.Slug=b.Slug;
INSERT dbo.CmsPublications(Slug,VersionId) SELECT Slug,PublishedVersionId FROM dbo.CmsBlocks;
GO
