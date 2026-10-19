using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Helpers;
using PhoneStore.Models;
using PhoneStore.Services;
using PhoneStore.ViewModels;

namespace PhoneStore.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class ProductsController(ApplicationDbContext db, ImageService images) : Controller
{
    public async Task<IActionResult> Index(string? keyword, int? brandId, bool? active, int page = 1)
    {
        var query = db.Products.AsNoTracking().Include(p => p.Brand).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(p => p.ProductName.Contains(keyword));
        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId);
        if (active.HasValue) query = query.Where(p => p.IsActive == active);
        var count = await query.CountAsync(); page = Math.Clamp(page, 1, Math.Max(1, (count + 11) / 12));
        ViewData["Page"] = page; ViewData["Pages"] = (count + 11) / 12; ViewBag.Brands = await db.Brands.ToListAsync();
        return View(await query.OrderByDescending(p => p.ProductId).Skip((page - 1) * 12).Take(12).ToListAsync());
    }
    public async Task<IActionResult> Create() => View("Edit", await Populate(new ProductEditViewModel()));
    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Products.Include(p => p.Images).SingleOrDefaultAsync(p => p.ProductId == id);
        if (p == null) return NotFound();
        return View(await Populate(new ProductEditViewModel { ProductId = p.ProductId, ProductName = p.ProductName, Description = p.Description, Price = p.Price, DiscountPrice = p.DiscountPrice, StockQuantity = p.StockQuantity, BrandId = p.BrandId, CategoryId = p.CategoryId, Ram = p.Ram, Storage = p.Storage, Color = p.Color, Screen = p.Screen, Processor = p.Processor, Battery = p.Battery, Camera = p.Camera, IsActive = p.IsActive, IsFeatured = p.IsFeatured, RowVersion = Convert.ToBase64String(p.RowVersion), Images = p.Images }));
    }
    [HttpPost, RequestSizeLimit(45000000)]
    public Task<IActionResult> Create(ProductEditViewModel model) { model.ProductId = 0; return Save(model); }
    [HttpPost, RequestSizeLimit(45000000)]
    public Task<IActionResult> Edit(ProductEditViewModel model) => Save(model);
    private async Task<IActionResult> Save(ProductEditViewModel model)
    {
        if (!await db.Brands.AnyAsync(b => b.BrandId == model.BrandId) || !await db.Categories.AnyAsync(c => c.CategoryId == model.CategoryId)) ModelState.AddModelError("", "Danh mục hoặc thương hiệu không hợp lệ.");
        try { foreach (var upload in model.Uploads ?? []) await images.ValidateAsync(upload); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("Uploads", ex.Message); }
        if (!ModelState.IsValid) return View("Edit", await Populate(model));
        var p = model.ProductId == 0 ? new Product { Slug = StoreHelper.Slug(model.ProductName) } : await db.Products.Include(p => p.Images).SingleOrDefaultAsync(p => p.ProductId == model.ProductId);
        if (p == null) return NotFound();
        if (p.ProductId != 0)
        {
            if (model.RowVersion != Convert.ToBase64String(p.RowVersion)) { ModelState.AddModelError("", "Sản phẩm hoặc tồn kho vừa thay đổi. Tải lại trang trước khi lưu."); return View("Edit", await Populate(model)); }
        }
        p.ProductName = model.ProductName; p.Description = model.Description; p.Price = model.Price; p.DiscountPrice = model.DiscountPrice;
        p.StockQuantity = model.StockQuantity; p.BrandId = model.BrandId; p.CategoryId = model.CategoryId; p.Ram = model.Ram; p.Storage = model.Storage; p.Color = model.Color;
        p.Screen = model.Screen; p.Processor = model.Processor; p.Battery = model.Battery; p.Camera = model.Camera; p.IsActive = model.IsActive; p.IsFeatured = model.IsFeatured; p.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = new List<string>();
        try
        {
            foreach (var upload in model.Uploads ?? [])
            {
                var path = await images.SaveAsync(upload); saved.Add(path);
                var primary = p.Images.Count == 0;
                p.Images.Add(new ProductImage { ImageUrl = path, IsPrimary = primary, DisplayOrder = p.Images.Count });
                if (primary) p.MainImageUrl = path;
            }
            if (p.ProductId == 0) db.Products.Add(p);
            await db.SaveChangesAsync();
            TempData["Success"] = "Đã lưu sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is DbUpdateException or IOException)
        {
            foreach (var path in saved) images.Delete(path);
            ModelState.AddModelError("", "Không thể lưu. Dữ liệu có thể vừa thay đổi, vui lòng tải lại và thử lại.");
            return View("Edit", await Populate(model));
        }
    }
    public async Task<IActionResult> Details(int id)
    {
        var p = await db.Products.AsNoTracking().Include(p => p.Brand).Include(p => p.Category).Include(p => p.Images).SingleOrDefaultAsync(p => p.ProductId == id);
        return p == null ? NotFound() : View(p);
    }
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var p = await db.Products.FindAsync(id); if (p == null) return NotFound();
        p.IsActive = !p.IsActive;
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { TempData["Error"] = "Dữ liệu vừa thay đổi. Vui lòng thử lại."; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> RemoveImage(int id)
    {
        var image = await db.ProductImages.Include(i => i.Product).ThenInclude(p => p.Images).SingleOrDefaultAsync(i => i.ProductImageId == id);
        if (image == null) return NotFound();
        var product = image.Product;
        db.ProductImages.Remove(image);
        if (image.IsPrimary)
        {
            var next = product.Images.Where(i => i.ProductImageId != id).OrderBy(i => i.DisplayOrder).FirstOrDefault();
            if (next != null) next.IsPrimary = true;
            product.MainImageUrl = next?.ImageUrl ?? "/images/products/placeholder.svg";
        }
        try { await db.SaveChangesAsync(); images.Delete(image.ImageUrl); }
        catch (DbUpdateException) { TempData["Error"] = "Không thể xóa ảnh. Vui lòng tải lại."; }
        return RedirectToAction(nameof(Edit), new { id = product.ProductId });
    }
    private async Task<ProductEditViewModel> Populate(ProductEditViewModel model)
    {
        model.Brands = await db.Brands.AsNoTracking().ToListAsync(); model.Categories = await db.Categories.AsNoTracking().ToListAsync();
        if (model.ProductId != 0) model.Images = await db.ProductImages.AsNoTracking().Where(i => i.ProductId == model.ProductId).ToListAsync();
        return model;
    }
}
