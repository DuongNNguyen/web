using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using PhoneStore.Services;

namespace PhoneStore.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class OrdersController(ApplicationDbContext db, OrderService orders) : Controller
{
    public async Task<IActionResult> Index(string? keyword, OrderStatus? status, string sort = "newest", int page = 1)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(o => o.OrderCode.Contains(keyword) || o.ReceiverName.Contains(keyword) || o.ReceiverEmail.Contains(keyword) || o.User.FullName.Contains(keyword));
        if (status.HasValue) query = query.Where(o => o.Status == status);
        var count = await query.CountAsync(); page = Math.Clamp(page, 1, Math.Max(1, (count + 11) / 12));
        ViewData["Page"] = page; ViewData["Pages"] = (count + 11) / 12;
        query = sort == "oldest" ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate);
        return View(await query.Skip((page - 1) * 12).Take(12).ToListAsync());
    }
    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.User).Include(o => o.Details).SingleOrDefaultAsync(o => o.OrderId == id);
        return order == null ? NotFound() : View(order);
    }
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        if (!ModelState.IsValid) return BadRequest();
        try { await orders.TransitionAsync(id, status); TempData["Success"] = "Đã cập nhật trạng thái đơn hàng."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (DbUpdateException) { TempData["Error"] = "Đơn hàng vừa thay đổi. Vui lòng tải lại."; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
