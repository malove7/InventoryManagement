using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
