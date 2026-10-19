using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.Services;
using PhoneStore.ViewModels;

namespace PhoneStore.Controllers;
[Authorize]
public class CheckoutController(CartService cart, OrderService orders, UserManager<ApplicationUser> users, ILogger<CheckoutController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var summary = await cart.GetAsync();
        if (summary.Lines.Count == 0) return RedirectToAction("Index", "Cart");
        var user = (await users.GetUserAsync(User))!;
        var token = Guid.NewGuid(); HttpContext.Session.SetString("CheckoutToken", token.ToString());
        return View(new CheckoutViewModel { ReceiverName = user.FullName, ReceiverEmail = user.Email!, ReceiverPhone = user.PhoneNumber ?? "", ShippingAddress = user.Address ?? "", Cart = summary, Token = token });
    }
    [HttpPost]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        if (model.Token.ToString() != HttpContext.Session.GetString("CheckoutToken")) ModelState.AddModelError("", "Phiên đặt hàng đã hết hạn. Vui lòng tải lại trang.");
        if (ModelState.IsValid)
        {
            try
            {
                var order = await orders.CreateAsync(users.GetUserId(User)!, cart.Read(), model);
                cart.Clear();
                TempData["Success"] = "Đặt hàng thành công! Cảm ơn bạn đã chọn PhoneStore.";
                return RedirectToAction("Details", "Orders", new { id = order.OrderId });
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            catch (DbUpdateException ex) { logger.LogWarning(ex, "Checkout conflict"); ModelState.AddModelError("", "Dữ liệu vừa thay đổi. Vui lòng kiểm tra giỏ hàng và thử lại."); }
        }
        model.Cart = await cart.GetAsync();
        return View(model);
    }
}
