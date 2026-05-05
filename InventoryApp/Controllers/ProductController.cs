using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers;

public class ProductController : Controller
{
    private readonly AppDbContext _db;

    public ProductController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, int? categoryId, string? stockFilter)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || (p.SKU != null && p.SKU.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (stockFilter == "low")
            query = query.Where(p => p.Quantity <= p.LowStockThreshold);
        else if (stockFilter == "out")
            query = query.Where(p => p.Quantity == 0);

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.StockFilter = stockFilter;
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();

        return View(await query.OrderBy(p => p.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        return View(product);
    }

    public async Task<IActionResult> History(int id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        var history = await _db.InventoryHistories
            .Where(h => h.ProductId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

        ViewBag.Product = product;
        return View(history);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            return View(product);
        }
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        if (product.Quantity > 0)
        {
            _db.InventoryHistories.Add(new InventoryHistory
            {
                ProductId = product.Id,
                ChangeType = InventoryChangeType.InitialStock,
                PreviousQuantity = 0,
                NewQuantity = product.Quantity,
                Reason = "Product created",
                ChangedAt = product.CreatedAt
            });
            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        TempData["Success"] = $"Product \"{product.Name}\" added successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        await PopulateCategoriesAsync(product.CategoryId);
        return View(product);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(product.CategoryId);
            return View(product);
        }

        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();

        var oldQty = existing.Quantity;

        existing.Name = product.Name;
        existing.SKU = product.SKU;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.Quantity = product.Quantity;
        existing.LowStockThreshold = product.LowStockThreshold;
        existing.CategoryId = product.CategoryId;
        existing.UpdatedAt = DateTime.UtcNow;

        if (oldQty != product.Quantity)
        {
            _db.InventoryHistories.Add(new InventoryHistory
            {
                ProductId = id,
                ChangeType = InventoryChangeType.ManualEdit,
                PreviousQuantity = oldQty,
                NewQuantity = product.Quantity,
                Reason = "Quantity updated via product edit",
                ChangedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Product \"{existing.Name}\" updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Product \"{product.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int id, int adjustment, string? reason)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var oldQty = product.Quantity;
        var newQty = oldQty + adjustment;
        if (newQty < 0)
        {
            TempData["Error"] = "Stock cannot go below zero.";
            return RedirectToAction(nameof(Details), new { id });
        }

        product.Quantity = newQty;
        product.UpdatedAt = DateTime.UtcNow;

        _db.InventoryHistories.Add(new InventoryHistory
        {
            ProductId = id,
            ChangeType = InventoryChangeType.StockAdjustment,
            PreviousQuantity = oldQty,
            NewQuantity = newQty,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Stock adjusted by {(adjustment >= 0 ? "+" : "")}{adjustment}. New quantity: {newQty}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> StockReport(int? categoryId, string? statusFilter)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (statusFilter == "low")
            query = query.Where(p => p.Quantity > 0 && p.Quantity <= p.LowStockThreshold);
        else if (statusFilter == "out")
            query = query.Where(p => p.Quantity == 0);

        var products = await query.OrderBy(p => p.Category!.Name).ThenBy(p => p.Name).ToListAsync();

        ViewBag.CategoryId = categoryId;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.TotalProducts = products.Count;
        ViewBag.TotalUnits = products.Sum(p => p.Quantity);
        ViewBag.TotalValue = products.Sum(p => p.TotalValue);
        ViewBag.LowStockCount = products.Count(p => p.Quantity > 0 && p.Quantity <= p.LowStockThreshold);
        ViewBag.OutOfStockCount = products.Count(p => p.Quantity == 0);

        return View(products);
    }

    private async Task PopulateCategoriesAsync(int? selectedId = null)
    {
        var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.CategoryId = new SelectList(categories, "Id", "Name", selectedId);
    }
}
