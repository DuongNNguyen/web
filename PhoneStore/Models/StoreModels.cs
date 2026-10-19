using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PhoneStore.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(500)] public string? Address { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class Category
{
    public int CategoryId { get; set; }
    [Required, MaxLength(120)] public string CategoryName { get; set; } = "";
    [Required, MaxLength(180)] public string Slug { get; set; } = "";
    [MaxLength(2000)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class Brand
{
    public int BrandId { get; set; }
    [Required, MaxLength(120)] public string BrandName { get; set; } = "";
    [Required, MaxLength(180)] public string Slug { get; set; } = "";
    [MaxLength(500)] public string? LogoUrl { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class Product
{
    public int ProductId { get; set; }
    [Required, MaxLength(180)] public string ProductName { get; set; } = "";
    [Required, MaxLength(220)] public string Slug { get; set; } = "";
    [MaxLength(10000)] public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    [MaxLength(500)] public string MainImageUrl { get; set; } = "/images/products/placeholder.svg";
    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    [MaxLength(120)] public string? Ram { get; set; }
    [MaxLength(120)] public string? Storage { get; set; }
    [MaxLength(120)] public string? Color { get; set; }
    [MaxLength(200)] public string? Screen { get; set; }
    [MaxLength(200)] public string? Processor { get; set; }
    [MaxLength(200)] public string? Battery { get; set; }
    [MaxLength(200)] public string? Camera { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public List<ProductImage> Images { get; set; } = [];
    [NotMapped] public decimal SellingPrice => DiscountPrice is > 0 && DiscountPrice < Price ? DiscountPrice.Value : Price;
}
public class ProductImage
{
    public int ProductImageId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [MaxLength(500)] public string ImageUrl { get; set; } = "";
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}
public enum OrderStatus
{
    [Display(Name = "Chờ xác nhận")] Pending,
    [Display(Name = "Đã xác nhận")] Confirmed,
    [Display(Name = "Đang giao hàng")] Shipping,
    [Display(Name = "Hoàn thành")] Completed,
    [Display(Name = "Đã hủy")] Cancelled
}
public class Order
{
    public int OrderId { get; set; }
    [MaxLength(60)] public string OrderCode { get; set; } = "";
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    [MaxLength(120)] public string ReceiverName { get; set; } = "";
    [MaxLength(30)] public string ReceiverPhone { get; set; } = "";
    [MaxLength(256)] public string ReceiverEmail { get; set; } = "";
    [MaxLength(500)] public string ShippingAddress { get; set; } = "";
    [MaxLength(2000)] public string? Note { get; set; }
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    [MaxLength(30)] public string PaymentMethod { get; set; } = "COD";
    public Guid CheckoutToken { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public List<OrderDetail> Details { get; set; } = [];
}
public class OrderDetail
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [MaxLength(180)] public string ProductNameSnapshot { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    [NotMapped] public decimal Subtotal => UnitPrice * Quantity;
}
