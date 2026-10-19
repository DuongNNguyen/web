using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Controllers;
public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email);
        if (user != null && user.IsActive)
        {
            var result = await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
            if (result.Succeeded) return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");
        }
        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
        return View(model);
    }
    public IActionResult Register() => View(new RegisterViewModel());
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, PhoneNumber = model.PhoneNumber };
        var result = await users.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            var role = await users.AddToRoleAsync(user, "Customer");
            if (!role.Succeeded) { await users.DeleteAsync(user); ModelState.AddModelError("", "Không thể tạo tài khoản. Vui lòng thử lại."); return View(model); }
            await signIn.SignInAsync(user, false);
            return RedirectToAction("Index", "Home");
        }
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }
    [Authorize, HttpPost]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = (await users.GetUserAsync(User))!;
        return View(new ProfileViewModel { FullName = user.FullName, PhoneNumber = user.PhoneNumber ?? "", Address = user.Address });
    }
    [Authorize, HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = (await users.GetUserAsync(User))!;
        user.FullName = model.FullName; user.PhoneNumber = model.PhoneNumber; user.Address = model.Address;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(model); }
        TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
        return RedirectToAction(nameof(Profile));
    }
    [Authorize]
    public IActionResult ChangePassword() => View(new PasswordViewModel());
    [Authorize, HttpPost]
    public async Task<IActionResult> ChangePassword(PasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = (await users.GetUserAsync(User))!;
        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded) { await signIn.RefreshSignInAsync(user); TempData["Success"] = "Đã đổi mật khẩu."; return RedirectToAction(nameof(Profile)); }
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }
    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }
}
