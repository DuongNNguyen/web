using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Helpers;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class BrandsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Brands.AsNoTracking().OrderBy(x => x.BrandName).ToListAsync());
    public IActionResult Create() => View("Edit", new LookupViewModel());
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Brands.FindAsync(id);
        return item == null ? NotFound() : View(new LookupViewModel { Id = id, Name = item.BrandName, Description = item.Description, IsActive = item.IsActive });
    }
    [HttpPost] public Task<IActionResult> Create(LookupViewModel model) { model.Id = 0; return Save(model); }
    [HttpPost] public Task<IActionResult> Edit(LookupViewModel model) => Save(model);
    private async Task<IActionResult> Save(LookupViewModel model)
    {
        if (!ModelState.IsValid) return View("Edit", model);
        var item = model.Id == 0 ? new Brand { Slug = StoreHelper.Slug(model.Name) } : await db.Brands.FindAsync(model.Id);
        if (item == null) return NotFound();
        item.BrandName = model.Name; item.Description = model.Description; item.IsActive = model.IsActive;
        if (model.Id == 0) db.Brands.Add(item);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Không thể lưu dữ liệu. Vui lòng thử lại."); return View("Edit", model); }
        TempData["Success"] = "Đã lưu thành công.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.Brands.FindAsync(id); if (item == null) return NotFound();
        item.IsActive = !item.IsActive; await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
