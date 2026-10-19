using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Controllers;
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var query = db.Products.AsNoTracking().Include(p => p.Brand).Where(p => p.IsActive && p.Brand.IsActive && p.Category.IsActive);
        return View(new HomeViewModel {
            Featured = await query.Where(p => p.IsFeatured).OrderBy(p => p.ProductId).Take(8).ToListAsync(),
            Newest = await query.OrderByDescending(p => p.CreatedAt).Take(4).ToListAsync(),
            Sale = await query.Where(p => p.DiscountPrice > 0 && p.DiscountPrice < p.Price).OrderBy(p => p.ProductId).Take(4).ToListAsync(),
            Brands = await db.Brands.Where(b => b.IsActive).ToListAsync()
        });
    }
    public IActionResult Privacy() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    public IActionResult Status(int code) { Response.StatusCode = code; return View(code); }
}
