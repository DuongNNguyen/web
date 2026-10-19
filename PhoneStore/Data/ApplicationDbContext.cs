using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;

namespace PhoneStore.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Brand>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Product>().HasIndex(x => x.ProductName);
        b.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<Product>().Property(x => x.DiscountPrice).HasPrecision(18, 2);
        b.Entity<Product>().ToTable(t => { t.HasCheckConstraint("CK_Product_Stock", "[StockQuantity] >= 0"); t.HasCheckConstraint("CK_Product_Price", "[Price] >= 0 AND ([DiscountPrice] IS NULL OR [DiscountPrice] >= 0)"); });
        b.Entity<Product>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>().HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>().HasIndex(x => x.OrderCode).IsUnique();
        b.Entity<Order>().HasIndex(x => x.CheckoutToken).IsUnique();
        b.Entity<Order>().HasIndex(x => new { x.Status, x.OrderDate });
        b.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Entity<Order>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderDetail>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Entity<OrderDetail>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderDetail>().HasOne(x => x.Order).WithMany(x => x.Details).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderDetail>().ToTable(t => t.HasCheckConstraint("CK_OrderDetail_Quantity", "[Quantity] > 0"));
    }
}
