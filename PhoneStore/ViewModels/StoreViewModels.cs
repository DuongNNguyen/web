using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PhoneStore.Models;

namespace PhoneStore.ViewModels;
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = "";
    [Display(Name = "Ghi nhớ đăng nhập")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
public class RegisterViewModel
{
    [Required, StringLength(120), Display(Name = "Họ và tên")] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(30), Display(Name = "Số điện thoại")] public string PhoneNumber { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = "";
    [Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu")] public string ConfirmPassword { get; set; } = "";
}
public class ProfileViewModel
{
    [Required, StringLength(120), Display(Name = "Họ và tên")] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(30), Display(Name = "Số điện thoại")] public string PhoneNumber { get; set; } = "";
    [StringLength(500), Display(Name = "Địa chỉ")] public string? Address { get; set; }
}
public class PasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu hiện tại")] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Mật khẩu mới")] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu mới")] public string ConfirmPassword { get; set; } = "";
}
public class CatalogViewModel
{
    public List<Product> Products { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<Brand> Brands { get; set; } = [];
    public string? Keyword { get; set; }
    public int? BrandId { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStock { get; set; }
    public bool Sale { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int Total { get; set; }
}
public class HomeViewModel
{
    public List<Product> Featured { get; set; } = [];
    public List<Product> Newest { get; set; } = [];
    public List<Product> Sale { get; set; } = [];
    public List<Brand> Brands { get; set; } = [];
}
public record CartLine(Product Product, int Quantity)
{
    public decimal Subtotal => Product.SellingPrice * Quantity;
    public bool Available => Product.IsActive && Product.Category.IsActive && Product.Brand.IsActive && Product.StockQuantity >= Quantity;
}
public class CartViewModel
{
    public List<CartLine> Lines { get; set; } = [];
    public decimal Total => Lines.Sum(x => x.Subtotal);
    public int Quantity => Lines.Sum(x => x.Quantity);
}
public class CheckoutViewModel
{
    [Required, StringLength(120), Display(Name = "Họ tên người nhận")] public string ReceiverName { get; set; } = "";
    [Required, Phone, StringLength(30), Display(Name = "Số điện thoại")] public string ReceiverPhone { get; set; } = "";
    [Required, EmailAddress, StringLength(256), Display(Name = "Email")] public string ReceiverEmail { get; set; } = "";
    [Required, StringLength(500), Display(Name = "Địa chỉ nhận hàng")] public string ShippingAddress { get; set; } = "";
    [StringLength(2000), Display(Name = "Ghi chú")] public string? Note { get; set; }
    public Guid Token { get; set; }
    [ValidateNever] public CartViewModel Cart { get; set; } = new();
}
public class LookupViewModel
{
    public int Id { get; set; }
    [Required, StringLength(120), Display(Name = "Tên")] public string Name { get; set; } = "";
    [StringLength(2000), Display(Name = "Mô tả")] public string? Description { get; set; }
    [Display(Name = "Hoạt động")] public bool IsActive { get; set; } = true;
}
public class ProductEditViewModel : IValidatableObject
{
    public int ProductId { get; set; }
    [Required, StringLength(180), Display(Name = "Tên sản phẩm")] public string ProductName { get; set; } = "";
    [StringLength(10000), Display(Name = "Mô tả")] public string? Description { get; set; }
    [Range(typeof(decimal), "0", "1000000000000"), Display(Name = "Giá gốc (₫)")] public decimal Price { get; set; }
    [Range(typeof(decimal), "0", "1000000000000"), Display(Name = "Giá khuyến mãi (₫)")] public decimal? DiscountPrice { get; set; }
    [Range(0, 1000000), Display(Name = "Tồn kho")] public int StockQuantity { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Thương hiệu")] public int BrandId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Danh mục")] public int CategoryId { get; set; }
    [StringLength(120), Display(Name = "RAM")] public string? Ram { get; set; }
    [StringLength(120), Display(Name = "Bộ nhớ")] public string? Storage { get; set; }
    [StringLength(120), Display(Name = "Màu sắc")] public string? Color { get; set; }
    [StringLength(200), Display(Name = "Màn hình")] public string? Screen { get; set; }
    [StringLength(200), Display(Name = "Vi xử lý")] public string? Processor { get; set; }
    [StringLength(200), Display(Name = "Pin")] public string? Battery { get; set; }
    [StringLength(200)] public string? Camera { get; set; }
    [Display(Name = "Hoạt động")] public bool IsActive { get; set; } = true;
    [Display(Name = "Nổi bật")] public bool IsFeatured { get; set; }
    public string? RowVersion { get; set; }
    [Display(Name = "Ảnh sản phẩm (tối đa 8 ảnh, 5 MB/ảnh)")] public List<IFormFile>? Uploads { get; set; }
    [ValidateNever] public List<Brand> Brands { get; set; } = [];
    [ValidateNever] public List<Category> Categories { get; set; } = [];
    [ValidateNever] public List<ProductImage> Images { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (DiscountPrice.HasValue && DiscountPrice >= Price) yield return new ValidationResult("Giá khuyến mãi phải nhỏ hơn giá gốc.", [nameof(DiscountPrice)]);
        if (Uploads?.Count > 8) yield return new ValidationResult("Chọn tối đa 8 ảnh mỗi lần.", [nameof(Uploads)]);
    }
}
public class DashboardViewModel
{
    public int Products { get; set; }
    public int Customers { get; set; }
    public int Orders { get; set; }
    public int Pending { get; set; }
    public int LowStock { get; set; }
    public decimal Revenue { get; set; }
    public List<Order> Recent { get; set; } = [];
    public int[] MonthlyOrders { get; set; } = new int[12];
    public decimal[] MonthlyRevenue { get; set; } = new decimal[12];
    public int Year { get; set; }
}
public record UserRow(string Id, string FullName, string? Email, string? Phone, string Roles, DateTimeOffset CreatedAt, bool IsActive, bool IsAdmin);
