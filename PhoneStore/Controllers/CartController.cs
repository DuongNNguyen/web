using Microsoft.AspNetCore.Mvc;
using PhoneStore.Services;

namespace PhoneStore.Controllers;
public class CartController(CartService cart) : Controller
{
    public async Task<IActionResult> Index() => View(await cart.GetAsync());
    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Số lượng không hợp lệ."; return RedirectToAction(nameof(Index)); }
        try { await cart.SetAsync(productId, quantity, true); TempData["Success"] = "Thêm vào giỏ hàng thành công."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Số lượng không hợp lệ."; return RedirectToAction(nameof(Index)); }
        try { await cart.SetAsync(productId, quantity); TempData["Success"] = "Đã cập nhật giỏ hàng."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public IActionResult Remove(int productId) { cart.Remove(productId); return RedirectToAction(nameof(Index)); }
    [HttpPost] public IActionResult Clear() { cart.Clear(); return RedirectToAction(nameof(Index)); }
}
