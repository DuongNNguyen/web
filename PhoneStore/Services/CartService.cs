using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.ViewModels;

namespace PhoneStore.Services;
public class CartService(IHttpContextAccessor accessor, ApplicationDbContext db)
{
    private ISession Session => accessor.HttpContext!.Session;
    public Dictionary<int, int> Read() => JsonSerializer.Deserialize<Dictionary<int, int>>(Session.GetString("Cart") ?? "{}") ?? [];
    public void Save(Dictionary<int, int> items) => Session.SetString("Cart", JsonSerializer.Serialize(items));
    public void Clear() => Session.Remove("Cart");
    public async Task<CartViewModel> GetAsync()
    {
        var items = Read();
        var products = await db.Products.AsNoTracking().Include(x => x.Brand).Include(x => x.Category).Where(x => items.Keys.Contains(x.ProductId)).ToListAsync();
        return new() { Lines = products.Select(p => new CartLine(p, items[p.ProductId])).ToList() };
    }
    public async Task SetAsync(int productId, int quantity, bool add = false)
    {
        var items = Read();
        var p = await db.Products.Include(x => x.Brand).Include(x => x.Category).SingleOrDefaultAsync(x => x.ProductId == productId);
        if (p is null || !p.IsActive || !p.Brand.IsActive || !p.Category.IsActive) throw new InvalidOperationException("Sản phẩm không còn được bán.");
        var total = (long)quantity + (add ? items.GetValueOrDefault(productId) : 0);
        if (quantity < 1 || total > p.StockQuantity) throw new InvalidOperationException("Số lượng không hợp lệ hoặc vượt quá tồn kho.");
        if (items.Count >= 100 && !items.ContainsKey(productId)) throw new InvalidOperationException("Giỏ hàng tối đa 100 sản phẩm khác nhau.");
        items[productId] = (int)total;
        Save(items);
    }
    public void Remove(int id) { var items = Read(); items.Remove(id); Save(items); }
}
