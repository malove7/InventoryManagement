using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models;

public enum InventoryChangeType
{
    InitialStock,
    ManualEdit,
    StockAdjustment
}

public class InventoryHistory
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public InventoryChangeType ChangeType { get; set; }

    public int PreviousQuantity { get; set; }

    public int NewQuantity { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public DateTime ChangedAt { get; set; }
}