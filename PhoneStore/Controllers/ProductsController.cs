using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.ViewModels;

namespace PhoneStore.Controllers;
public class ProductsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CatalogViewModel model)
    {
        var query = db.Products.AsNoTracking().Include(p => p.Brand).Include(p => p.Category).Where(p => p.IsActive && p.Brand.IsActive && p.Category.IsActive);
        if (!string.IsNullOrWhiteSpace(model.Keyword)) { var term = model.Keyword.Trim(); query = query.Where(p => p.ProductName.Contains(term) || p.Brand.BrandName.Contains(term) || (p.Description != null && p.Description.Contains(term))); }
        if (model.BrandId.HasValue) query = query.Where(p => p.BrandId == model.BrandId);
        if (model.CategoryId.HasValue) query = query.Where(p => p.CategoryId == model.CategoryId);
        if (model.MinPrice.HasValue) query = query.Where(p => (p.DiscountPrice > 0 && p.DiscountPrice < p.Price ? p.DiscountPrice.Value : p.Price) >= model.MinPrice);
        if (model.MaxPrice.HasValue) query = query.Where(p => (p.DiscountPrice > 0 && p.DiscountPrice < p.Price ? p.DiscountPrice.Value : p.Price) <= model.MaxPrice);
        if (model.InStock) query = query.Where(p => p.StockQuantity > 0);
        if (model.Sale) query = query.Where(p => p.DiscountPrice > 0 && p.DiscountPrice < p.Price);
        query = model.Sort switch {
            "price_asc" => query.OrderBy(p => p.DiscountPrice > 0 && p.DiscountPrice < p.Price ? p.DiscountPrice.Value : p.Price).ThenBy(p => p.ProductId),
            "price_desc" => query.OrderByDescending(p => p.DiscountPrice > 0 && p.DiscountPrice < p.Price ? p.DiscountPrice.Value : p.Price).ThenBy(p => p.ProductId),
            "name_asc" => query.OrderBy(p => p.ProductName).ThenBy(p => p.ProductId),
            "name_desc" => query.OrderByDescending(p => p.ProductName).ThenBy(p => p.ProductId),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.ProductId) };
        model.Total = await query.CountAsync();
        model.Page = Math.Clamp(model.Page, 1, Math.Max(1, (model.Total + 11) / 12));
        model.Products = await query.Skip((model.Page - 1) * 12).Take(12).ToListAsync();
        model.Brands = await db.Brands.Where(b => b.IsActive).ToListAsync();
        model.Categories = await db.Categories.Where(c => c.IsActive).ToListAsync();
        ViewData["Page"] = model.Page; ViewData["Pages"] = (model.Total + 11) / 12;
        return View(model);
    }
    [HttpGet("san-pham/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var product = await db.Products.AsNoTracking().Include(p => p.Brand).Include(p => p.Category).Include(p => p.Images).SingleOrDefaultAsync(p => p.Slug == slug && p.IsActive && p.Brand.IsActive && p.Category.IsActive);
        return product == null ? NotFound() : View(product);
    }
}
