using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Helpers;
using PhoneStore.Models;

namespace PhoneStore.Data;
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Customer" })
            if (!await roles.RoleExistsAsync(role)) Ensure(await roles.CreateAsync(new IdentityRole(role)));
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var email = config["SeedAdmin:Email"] ?? throw new InvalidOperationException("Missing development admin configuration.");
        var admin = await users.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, FullName = "Quản trị PhoneStore", EmailConfirmed = true };
            Ensure(await users.CreateAsync(admin, config["SeedAdmin:Password"]!));
        }
        if (!await users.IsInRoleAsync(admin, "Admin")) Ensure(await users.AddToRoleAsync(admin, "Admin"));
        foreach (var name in new[] { "Điện thoại", "Tablet", "Phụ kiện" })
            if (!await db.Categories.AnyAsync(x => x.CategoryName == name)) db.Categories.Add(new Category { CategoryName = name, Slug = StoreHelper.Slug(name) });
        foreach (var name in new[] { "Apple", "Samsung", "Xiaomi", "OPPO", "vivo", "realme" })
            if (!await db.Brands.AnyAsync(x => x.BrandName == name)) db.Brands.Add(new Brand { BrandName = name, Slug = name.ToLowerInvariant() });
        await db.SaveChangesAsync();
        if (await db.Products.AnyAsync()) return;
        var categories = await db.Categories.ToDictionaryAsync(x => x.CategoryName);
        var brands = await db.Brands.ToDictionaryAsync(x => x.BrandName);
        var samples = new (string Name, string Brand, decimal Price)[] {
            ("iPhone 16", "Apple", 22990000), ("iPhone 16 Pro", "Apple", 28990000), ("iPhone 16 Pro Max", "Apple", 34990000),
            ("Samsung Galaxy S25", "Samsung", 21990000), ("Samsung Galaxy S25 Ultra", "Samsung", 32990000),
            ("Xiaomi 15", "Xiaomi", 18990000), ("OPPO Find X8", "OPPO", 19990000), ("vivo X200", "vivo", 17990000),
            ("realme GT 7", "realme", 14990000), ("Samsung Galaxy A56", "Samsung", 9990000), ("Xiaomi Redmi Note 14", "Xiaomi", 5990000),
            ("OPPO Reno 13", "OPPO", 11990000), ("iPad Air", "Apple", 16990000), ("Tai nghe không dây", "Samsung", 1990000), ("vivo V50", "vivo", 12990000)
        };
        for (var i = 0; i < samples.Length; i++)
        {
            var s = samples[i];
            db.Products.Add(new Product { ProductName = s.Name, Slug = StoreHelper.Slug(s.Name), BrandId = brands[s.Brand].BrandId,
                CategoryId = categories[i == 12 ? "Tablet" : i == 13 ? "Phụ kiện" : "Điện thoại"].CategoryId,
                Price = s.Price, DiscountPrice = i % 3 == 0 ? s.Price - 1000000 : null, StockQuantity = i == 14 ? 0 : 5 + i,
                IsFeatured = i < 8, Ram = "8 GB", Storage = "256 GB", Color = "Đen", Screen = "OLED 6.5 inch", Processor = "Chip hiệu năng cao",
                Battery = "5000 mAh", Camera = "50 MP", Description = "Sản phẩm minh họa phục vụ đồ án PhoneStore. Giá và thông số là dữ liệu demo, không phải thông tin bán hàng thực tế.",
                MainImageUrl = $"/images/products/demo-{i % 4}.svg" });
        }
        await db.SaveChangesAsync();
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
