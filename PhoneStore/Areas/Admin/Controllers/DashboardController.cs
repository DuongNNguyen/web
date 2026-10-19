using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var year = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Year;
        var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));
        var model = new DashboardViewModel {
            Products = await db.Products.CountAsync(),
            Customers = await (from u in db.Users join ur in db.UserRoles on u.Id equals ur.UserId join r in db.Roles on ur.RoleId equals r.Id where r.Name == "Customer" select u.Id).CountAsync(),
            Orders = await db.Orders.CountAsync(), Pending = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
            LowStock = await db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= 5),
            Revenue = await db.Orders.Where(o => o.Status == OrderStatus.Completed).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            Recent = await db.Orders.AsNoTracking().OrderByDescending(o => o.OrderDate).Take(6).ToListAsync(), Year = year
        };
        var monthly = await db.Orders.Where(o => o.OrderDate >= start && o.OrderDate < start.AddYears(1))
            .GroupBy(o => o.OrderDate.AddHours(7).Month).Select(g => new { Month = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.Status == OrderStatus.Completed ? o.TotalAmount : 0) }).ToListAsync();
        foreach (var m in monthly) { model.MonthlyOrders[m.Month - 1] = m.Count; model.MonthlyRevenue[m.Month - 1] = m.Revenue; }
        return View(model);
    }
}
