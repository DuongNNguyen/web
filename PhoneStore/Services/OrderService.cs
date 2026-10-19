using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Services;
public class OrderService(ApplicationDbContext db)
{
    public async Task<Order> CreateAsync(string userId, Dictionary<int, int> cart, CheckoutViewModel input)
    {
        var existing = await db.Orders.SingleOrDefaultAsync(o => o.CheckoutToken == input.Token && o.UserId == userId);
        if (existing != null) return existing;
        if (cart.Count == 0 || input.Token == Guid.Empty) throw new InvalidOperationException("Giỏ hàng trống hoặc phiên đặt hàng không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var products = await db.Products.Include(p => p.Brand).Include(p => p.Category).Where(p => cart.Keys.Contains(p.ProductId)).OrderBy(p => p.ProductId).ToListAsync();
        if (products.Count != cart.Count) throw new InvalidOperationException("Sản phẩm trong giỏ hàng không còn tồn tại.");
        var order = new Order { UserId = userId, OrderCode = $"ORD{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}", ReceiverName = input.ReceiverName, ReceiverPhone = input.ReceiverPhone, ReceiverEmail = input.ReceiverEmail, ShippingAddress = input.ShippingAddress, Note = input.Note, CheckoutToken = input.Token };
        foreach (var p in products)
        {
            var quantity = cart[p.ProductId];
            if (!p.IsActive || !p.Brand.IsActive || !p.Category.IsActive || quantity < 1 || quantity > p.StockQuantity) throw new InvalidOperationException($"{p.ProductName}: không đủ tồn kho hoặc ngừng bán. Vui lòng cập nhật giỏ hàng.");
            order.Details.Add(new OrderDetail { ProductId = p.ProductId, ProductNameSnapshot = p.ProductName, UnitPrice = p.SellingPrice, Quantity = quantity });
            p.StockQuantity -= quantity;
        }
        order.TotalAmount = order.Details.Sum(d => d.Subtotal);
        db.Orders.Add(order);
        // RowVersion on products detects competing checkouts; the transaction rolls back all items.
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return order;
    }

    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) is
        (OrderStatus.Pending, OrderStatus.Confirmed) or (OrderStatus.Pending, OrderStatus.Cancelled) or
        (OrderStatus.Confirmed, OrderStatus.Shipping) or (OrderStatus.Shipping, OrderStatus.Completed);

    public async Task TransitionAsync(int id, OrderStatus status, string? customerId = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var order = await db.Orders.Include(o => o.Details).ThenInclude(d => d.Product).SingleOrDefaultAsync(o => o.OrderId == id && (customerId == null || o.UserId == customerId));
        if (order == null) throw new InvalidOperationException("Không tìm thấy đơn hàng.");
        if (!CanTransition(order.Status, status) || (customerId != null && status != OrderStatus.Cancelled)) throw new InvalidOperationException("Không thể chuyển trạng thái đơn hàng.");
        if (status == OrderStatus.Cancelled)
            foreach (var line in order.Details) line.Product.StockQuantity += line.Quantity;
        order.Status = status;
        // Order row version ensures cancellation restores stock exactly once.
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
