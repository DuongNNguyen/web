using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PhoneStore.Models;

namespace PhoneStore.Helpers;
public static class StoreHelper
{
    public static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string Date(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm");
    public static string Status(OrderStatus status) => status switch { OrderStatus.Pending => "Chờ xác nhận", OrderStatus.Confirmed => "Đã xác nhận", OrderStatus.Shipping => "Đang giao hàng", OrderStatus.Completed => "Hoàn thành", _ => "Đã hủy" };
    public static string Slug(string value)
    {
        var text = new string(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant().Replace('đ', 'd');
        text = Regex.Replace(text, "[^a-z0-9]+", "-").Trim('-');
        return (text.Length == 0 ? "item" : text)[..Math.Min(text.Length == 0 ? 4 : text.Length, 140)] + "-" + Guid.NewGuid().ToString("N")[..8];
    }
}
