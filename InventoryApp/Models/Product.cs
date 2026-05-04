using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApp.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SKU { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive value.")]
    [Display(Name = "Stock Quantity")]
    public int Quantity { get; set; }

    [Display(Name = "Low Stock Alert")]
    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; } = 10;

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsLowStock => Quantity <= LowStockThreshold;

    [NotMapped]
    public decimal TotalValue => Price * Quantity;
}
