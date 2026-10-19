using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(string? keyword, int page = 1)
    {
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(u => u.FullName.Contains(keyword) || u.Email!.Contains(keyword));
        var count = await query.CountAsync(); page = Math.Clamp(page, 1, Math.Max(1, (count + 11) / 12));
        ViewData["Page"] = page; ViewData["Pages"] = (count + 11) / 12;
        var rows = new List<UserRow>();
        foreach (var user in await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * 12).Take(12).ToListAsync())
        {
            var roles = await users.GetRolesAsync(user);
            rows.Add(new(user.Id, user.FullName, user.Email, user.PhoneNumber, string.Join(", ", roles), user.CreatedAt, user.IsActive, roles.Contains("Admin")));
        }
        return View(rows);
    }
    [HttpPost]
    public async Task<IActionResult> Toggle(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (user.Id == users.GetUserId(User) || await users.IsInRoleAsync(user, "Admin") || !await users.IsInRoleAsync(user, "Customer")) return Forbid();
        user.IsActive = !user.IsActive;
        var result = await users.UpdateAsync(user);
        if (result.Succeeded) { await users.UpdateSecurityStampAsync(user); TempData["Success"] = "Đã cập nhật tài khoản."; }
        else TempData["Error"] = "Không thể cập nhật tài khoản. Vui lòng thử lại.";
        return RedirectToAction(nameof(Index));
    }
}
