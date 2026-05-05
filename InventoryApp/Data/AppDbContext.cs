using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryHistory> InventoryHistories => Set<InventoryHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryHistory>()
            .Property(h => h.ChangeType)
            .HasConversion<string>();


        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and accessories", CreatedAt = new DateTime(2026, 1, 1) },
            new Category { Id = 2, Name = "Office Supplies", Description = "Stationery and office essentials", CreatedAt = new DateTime(2026, 1, 1) },
            new Category { Id = 3, Name = "Furniture", Description = "Office and home furniture", CreatedAt = new DateTime(2026, 1, 1) }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Wireless Mouse", SKU = "ELEC-001", Price = 29.99m, Quantity = 50, LowStockThreshold = 10, CategoryId = 1, CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 2, Name = "USB-C Hub", SKU = "ELEC-002", Price = 49.99m, Quantity = 8, LowStockThreshold = 10, CategoryId = 1, CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 3, Name = "Ballpoint Pens (12-pack)", SKU = "OFF-001", Price = 5.99m, Quantity = 200, LowStockThreshold = 20, CategoryId = 2, CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 4, Name = "Standing Desk", SKU = "FURN-001", Price = 349.99m, Quantity = 5, LowStockThreshold = 3, CategoryId = 3, CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 1) }
        );
    }
}
