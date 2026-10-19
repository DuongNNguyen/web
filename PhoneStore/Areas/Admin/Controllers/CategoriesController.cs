using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Helpers;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class CategoriesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Categories.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync());
    public IActionResult Create() => View("Edit", new LookupViewModel());
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Categories.FindAsync(id);
        return item == null ? NotFound() : View(new LookupViewModel { Id = id, Name = item.CategoryName, Description = item.Description, IsActive = item.IsActive });
    }
    [HttpPost] public Task<IActionResult> Create(LookupViewModel model) { model.Id = 0; return Save(model); }
    [HttpPost] public Task<IActionResult> Edit(LookupViewModel model) => Save(model);
    private async Task<IActionResult> Save(LookupViewModel model)
    {
        if (!ModelState.IsValid) return View("Edit", model);
        var item = model.Id == 0 ? new Category { Slug = StoreHelper.Slug(model.Name) } : await db.Categories.FindAsync(model.Id);
        if (item == null) return NotFound();
        item.CategoryName = model.Name; item.Description = model.Description; item.IsActive = model.IsActive;
        if (model.Id == 0) db.Categories.Add(item);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Không thể lưu dữ liệu. Vui lòng thử lại."); return View("Edit", model); }
        TempData["Success"] = "Đã lưu thành công.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.Categories.FindAsync(id); if (item == null) return NotFound();
        item.IsActive = !item.IsActive; await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
