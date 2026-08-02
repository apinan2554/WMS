using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Filters;
using WH_Logistic.Models;
using WH_Logistic.Services;

namespace WH_Logistic.Controllers
{
    [AuthorizeRole(UserRole.Admin, UserRole.WarehouseStaff, UserRole.InboundStaff, UserRole.FactoryManager)]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventory;
        private readonly IAuditService _audit;

        public InventoryController(ApplicationDbContext db, IInventoryService inventory, IAuditService audit)
        {
            _db = db;
            _inventory = inventory;
            _audit = audit;
        }

        // Stock Transfer
        public async Task<IActionResult> Transfer()
        {
            ViewBag.Products = await _db.Products.Include(p => p.Category).ToListAsync();
            ViewBag.Locations = await _db.Locations.Include(l => l.Zone).OrderBy(l => l.BinCode).ToListAsync();
            ViewBag.Transfers = await _db.StockTransfers
                .Include(t => t.Product)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .Include(t => t.TransferBy)
                .OrderByDescending(t => t.TransferDate)
                .Take(20)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(int productId, int fromLocationId, int toLocationId, int quantity)
        {
            var result = await _inventory.TransferStockAsync(productId, fromLocationId, toLocationId, quantity, GetCurrentUserId());

            if (!result.Success)
                TempData["Error"] = result.Message;
            else
                TempData["Success"] = result.Message;

            return RedirectToAction("Transfer");
        }

        // Cycle Count
        public async Task<IActionResult> CycleCount()
        {
            var counts = await _db.CycleCounts
                .Include(c => c.Location).ThenInclude(l => l!.Zone)
                .Include(c => c.CountBy)
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .OrderByDescending(c => c.CountDate)
                .ToListAsync();

            ViewBag.Locations = await _db.Locations.Include(l => l.Zone).OrderBy(l => l.BinCode).ToListAsync();
            return View(counts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCycleCount(int locationId)
        {
            var balances = await _db.InventoryBalances
                .Include(b => b.Product)
                .Where(b => b.LocationId == locationId)
                .ToListAsync();

            var cycleCount = new CycleCount
            {
                CountNumber = $"CC-{DateTime.UtcNow:yyyyMMddHHmmss}",
                LocationId = locationId,
                CountByUserId = GetCurrentUserId(),
                Status = CycleCountStatus.InProgress
            };

            foreach (var b in balances)
            {
                cycleCount.Items.Add(new CycleCountItem
                {
                    ProductId = b.ProductId,
                    SystemQty = b.Quantity,
                    ActualQty = b.Quantity // default same, staff will update
                });
            }

            _db.CycleCounts.Add(cycleCount);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentUserId(), "CREATE_CYCLE_COUNT", "CycleCount", $"สร้างรายการนับสต็อก {cycleCount.CountNumber}");

            return RedirectToAction("CycleCountDetail", new { id = cycleCount.CycleCountId });
        }

        public async Task<IActionResult> CycleCountDetail(int id)
        {
            var cycleCount = await _db.CycleCounts
                .Include(c => c.Location).ThenInclude(l => l!.Zone)
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.CycleCountId == id);

            if (cycleCount == null) return NotFound();
            return View(cycleCount);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitCycleCount(int cycleCountId, int[] countItemIds, int[] actualQtys)
        {
            var cycleCount = await _db.CycleCounts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CycleCountId == cycleCountId);

            if (cycleCount == null) return NotFound();

            for (int i = 0; i < countItemIds.Length; i++)
            {
                var item = cycleCount.Items.FirstOrDefault(x => x.CountItemId == countItemIds[i]);
                if (item != null)
                {
                    item.ActualQty = actualQtys[i];
                    if (item.Variance != 0)
                    {
                        await _inventory.AdjustStockAsync(item.ProductId, cycleCount.LocationId, actualQtys[i], GetCurrentUserId(), $"Cycle Count {cycleCount.CountNumber}");
                        item.IsAdjusted = true;
                    }
                }
            }

            cycleCount.Status = CycleCountStatus.Completed;
            await _db.SaveChangesAsync();
            TempData["Success"] = "บันทึกผลการนับสต็อกเรียบร้อยแล้ว";

            return RedirectToAction("CycleCount");
        }

        [HttpGet]
        public async Task<IActionResult> GetBalancesByProduct(int productId)
        {
            var balances = await _db.InventoryBalances
                .Include(b => b.Location).ThenInclude(l => l!.Zone)
                .Where(b => b.ProductId == productId && b.Quantity > 0)
                .ToListAsync();

            return Json(balances.Select(b => new
            {
                b.LocationId,
                b.Location!.BinCode,
                b.Location.Zone!.ZoneName,
                b.Quantity
            }));
        }

        private int GetCurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 1;
    }
}
