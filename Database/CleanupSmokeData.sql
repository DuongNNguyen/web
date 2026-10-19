-- One-time cleanup of the three verified local integration-test runs.
-- Exact run suffixes prevent matching unrelated demonstration records.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Runs TABLE (Suffix nvarchar(10) PRIMARY KEY);
    INSERT @Runs VALUES ('046e3398c0'), ('a7207bdacd'), ('8785592720');

    SELECT u.Id INTO #Users FROM AspNetUsers u
    WHERE EXISTS (SELECT 1 FROM @Runs r WHERE u.Email IN ('smoke-' + r.Suffix + '@example.test', 'other-' + r.Suffix + '@example.test'));
    SELECT p.ProductId INTO #Products FROM Products p
    WHERE EXISTS (SELECT 1 FROM @Runs r WHERE p.ProductName = 'Smoke Phone ' + r.Suffix);
    SELECT o.OrderId INTO #Orders FROM Orders o JOIN #Users u ON u.Id = o.UserId;
    SELECT c.CategoryId INTO #Categories FROM Categories c
    WHERE EXISTS (SELECT 1 FROM @Runs r WHERE c.CategoryName = 'Smoke Categories ' + r.Suffix + ' edited');
    SELECT b.BrandId INTO #Brands FROM Brands b
    WHERE EXISTS (SELECT 1 FROM @Runs r WHERE b.BrandName = 'Smoke Brands ' + r.Suffix + ' edited');

    IF EXISTS (SELECT 1 FROM #Users u JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE r.Name='Admin')
        THROW 51000, 'Refusing to delete an administrator.', 1;
    IF EXISTS (SELECT 1 FROM OrderDetails d JOIN #Orders o ON o.OrderId=d.OrderId WHERE NOT EXISTS (SELECT 1 FROM #Products p WHERE p.ProductId=d.ProductId))
        THROW 51001, 'Test order references a non-test product. Review inventory before cleanup.', 1;
    IF EXISTS (SELECT 1 FROM OrderDetails d JOIN #Products p ON p.ProductId=d.ProductId WHERE NOT EXISTS (SELECT 1 FROM #Orders o WHERE o.OrderId=d.OrderId))
        THROW 51002, 'Test product is referenced by a non-test order.', 1;

    -- Snapshot all retained entities; EXCEPT below verifies they remain unchanged.
    SELECT * INTO #KeepProducts FROM Products WHERE ProductId NOT IN (SELECT ProductId FROM #Products);
    SELECT * INTO #KeepCategories FROM Categories WHERE CategoryId NOT IN (SELECT CategoryId FROM #Categories);
    SELECT * INTO #KeepBrands FROM Brands WHERE BrandId NOT IN (SELECT BrandId FROM #Brands);
    SELECT * INTO #Admin FROM AspNetUsers WHERE Email='admin@phonestore.local';
    IF (SELECT COUNT(*) FROM #KeepProducts) <> 15 OR (SELECT COUNT(*) FROM #KeepCategories) <> 3 OR (SELECT COUNT(*) FROM #KeepBrands) <> 6 OR (SELECT COUNT(*) FROM #Admin) <> 1
        THROW 51003, 'Unexpected retained data. No cleanup performed.', 1;

    DELETE d FROM OrderDetails d JOIN #Orders o ON o.OrderId=d.OrderId;
    DELETE o FROM Orders o JOIN #Orders t ON t.OrderId=o.OrderId;
    DELETE i FROM ProductImages i JOIN #Products p ON p.ProductId=i.ProductId;
    DELETE p FROM Products p JOIN #Products t ON t.ProductId=p.ProductId;
    DELETE c FROM Categories c JOIN #Categories t ON t.CategoryId=c.CategoryId;
    DELETE b FROM Brands b JOIN #Brands t ON t.BrandId=b.BrandId;
    DELETE x FROM AspNetUserClaims x JOIN #Users u ON u.Id=x.UserId;
    DELETE x FROM AspNetUserLogins x JOIN #Users u ON u.Id=x.UserId;
    DELETE x FROM AspNetUserTokens x JOIN #Users u ON u.Id=x.UserId;
    DELETE x FROM AspNetUserRoles x JOIN #Users u ON u.Id=x.UserId;
    DELETE x FROM AspNetUsers x JOIN #Users u ON u.Id=x.Id;

    IF EXISTS (SELECT * FROM #KeepProducts EXCEPT SELECT * FROM Products)
       OR EXISTS (SELECT * FROM #KeepCategories EXCEPT SELECT * FROM Categories)
       OR EXISTS (SELECT * FROM #KeepBrands EXCEPT SELECT * FROM Brands)
       OR EXISTS (SELECT * FROM #Admin EXCEPT SELECT * FROM AspNetUsers)
        THROW 51004, 'Preserved data changed. Rolling back.', 1;
    IF NOT EXISTS (SELECT 1 FROM AspNetUsers u JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE u.Email='admin@phonestore.local' AND r.Name='Admin')
        THROW 51005, 'Default administrator role missing.', 1;

    SELECT (SELECT COUNT(*) FROM #Users) AS RemovedAccounts,
           (SELECT COUNT(*) FROM #Orders) AS RemovedOrders,
           (SELECT COUNT(*) FROM #Products) AS RemovedProducts,
           (SELECT COUNT(*) FROM #Categories) AS RemovedCategories,
           (SELECT COUNT(*) FROM #Brands) AS RemovedBrands;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
SELECT (SELECT COUNT(*) FROM AspNetUsers) AS RemainingAccounts,
       (SELECT COUNT(*) FROM Orders) AS RemainingOrders,
       (SELECT COUNT(*) FROM OrderDetails) AS RemainingOrderDetails,
       (SELECT COUNT(*) FROM Products) AS RemainingProducts,
       (SELECT COUNT(*) FROM Categories) AS RemainingCategories,
       (SELECT COUNT(*) FROM Brands) AS RemainingBrands;
