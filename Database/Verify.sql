SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM Products WHERE StockQuantity < 0)
    THROW 51000, 'Negative stock found', 1;
IF EXISTS (SELECT 1 FROM OrderDetails WHERE Quantity <= 0 OR UnitPrice < 0)
    THROW 51001, 'Invalid order detail found', 1;
IF EXISTS (
    SELECT o.OrderId FROM Orders o LEFT JOIN OrderDetails d ON d.OrderId = o.OrderId
    GROUP BY o.OrderId, o.TotalAmount
    HAVING COUNT(d.OrderDetailId) = 0 OR SUM(d.UnitPrice * d.Quantity) <> o.TotalAmount
)
    THROW 51002, 'Order totals do not match historical details', 1;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled = 1 OR is_not_trusted = 1)
    THROW 51003, 'Disabled or untrusted foreign key', 1;
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory;
SELECT COUNT(*) AS Products, MIN(StockQuantity) AS MinimumStock FROM Products;
SELECT COUNT(*) AS Orders, SUM(CASE WHEN Status = 3 THEN TotalAmount ELSE 0 END) AS CompletedRevenue FROM Orders;
SELECT COUNT(*) AS HistoricalDetails FROM OrderDetails;
SELECT 'PASS: stock, historical totals and foreign keys' AS Verification;
