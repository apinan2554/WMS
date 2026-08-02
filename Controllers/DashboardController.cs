using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Filters;
using WH_Logistic.Models;
using WH_Logistic.Services;

namespace WH_Logistic.Controllers
{
    [AuthorizeLogin]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventory;

        public DashboardController(ApplicationDbContext db, IInventoryService inventory)
        {
            _db = db;
            _inventory = inventory;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, int? zoneId)
        {
            var query = _db.InventoryBalances
                .Include(b => b.Product).ThenInclude(p => p!.Category)
                .Include(b => b.Location).ThenInclude(l => l!.Zone)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Product!.ProductName.Contains(search) || b.Product.SKU.Contains(search));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(b => b.Product!.CategoryId == categoryId.Value);
            }
            if (zoneId.HasValue)
            {
                query = query.Where(b => b.Location!.ZoneId == zoneId.Value);
            }

            var balances = await query.OrderBy(b => b.Location!.BinCode).ToListAsync();

            // Capacity stats
            var totalLocations = await _db.Locations.CountAsync();
            var usedLocations = await _db.InventoryBalances.Select(b => b.LocationId).Distinct().CountAsync();
            var capacityPercent = totalLocations > 0 ? (double)usedLocations / totalLocations * 100 : 0;

            var lowStockItems = await _inventory.GetLowStockItemsAsync();

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.ZoneId = zoneId;
            ViewBag.Categories = await _db.ProductCategories.ToListAsync();
            ViewBag.Zones = await _db.Zones.ToListAsync();
            ViewBag.CapacityPercent = Math.Round(capacityPercent, 1);
            ViewBag.TotalLocations = totalLocations;
            ViewBag.UsedLocations = usedLocations;
            ViewBag.LowStockItems = lowStockItems;
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalStock = await _db.InventoryBalances.SumAsync(b => b.Quantity);

            return View(balances);
        }
    }
}
