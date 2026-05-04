using InventoryApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var products = await _db.Products.Include(p => p.Category).ToListAsync();
        var categories = await _db.Categories.Include(c => c.Products).ToListAsync();

        ViewBag.TotalProducts = products.Count;
        ViewBag.TotalCategories = categories.Count;
        ViewBag.TotalStockValue = products.Sum(p => p.TotalValue);
        ViewBag.LowStockCount = products.Count(p => p.IsLowStock);
        ViewBag.LowStockItems = products.Where(p => p.IsLowStock).OrderBy(p => p.Quantity).Take(5).ToList();
        ViewBag.RecentProducts = products.OrderByDescending(p => p.CreatedAt).Take(5).ToList();
        ViewBag.CategoryBreakdown = categories
            .Select(c => new { c.Name, Count = c.Products.Count })
            .OrderByDescending(x => x.Count)
            .ToList();

        return View();
    }
}
