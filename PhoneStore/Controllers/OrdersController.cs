using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using PhoneStore.Services;

namespace PhoneStore.Controllers;
[Authorize]
public class OrdersController(ApplicationDbContext db, UserManager<ApplicationUser> users, OrderService orders) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.UserId == users.GetUserId(User));
        var count = await query.CountAsync(); page = Math.Clamp(page, 1, Math.Max(1, (count + 11) / 12));
        ViewData["Page"] = page; ViewData["Pages"] = (count + 11) / 12;
        return View(await query.OrderByDescending(o => o.OrderDate).Skip((page - 1) * 12).Take(12).ToListAsync());
    }
    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Details).SingleOrDefaultAsync(o => o.OrderId == id && o.UserId == users.GetUserId(User));
        return order == null ? NotFound() : View(order);
    }
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        try { await orders.TransitionAsync(id, OrderStatus.Cancelled, users.GetUserId(User)); TempData["Success"] = "Đơn hàng đã được hủy."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (DbUpdateException) { TempData["Error"] = "Đơn hàng vừa thay đổi. Vui lòng tải lại."; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
