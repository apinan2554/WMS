using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Filters;
using WH_Logistic.Models;
using WH_Logistic.Services;

namespace WH_Logistic.Controllers
{
    [AuthorizeRole(UserRole.Admin, UserRole.WarehouseStaff, UserRole.InboundStaff, UserRole.FactoryManager)]
    public class InboundController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventory;
        private readonly IAuditService _audit;

        public InboundController(ApplicationDbContext db, IInventoryService inventory, IAuditService audit)
        {
            _db = db;
            _inventory = inventory;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var grns = await _db.GoodsReceipts
                .Include(g => g.ReceivedBy)
                .Include(g => g.Items).ThenInclude(i => i.Product)
                .OrderByDescending(g => g.ReceivedDate)
                .ToListAsync();
            return View(grns);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _db.Products.Include(p => p.Category).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string poNumber, int[] productIds, int[] quantities)
        {
            var grn = new GoodsReceipt
            {
                GRNNumber = $"GRN-{DateTime.UtcNow:yyyyMMddHHmmss}",
                PONumber = poNumber,
                ReceivedByUserId = GetCurrentUserId(),
                Status = GRNStatus.Pending
            };

            for (int i = 0; i < productIds.Length; i++)
            {
                grn.Items.Add(new GoodsReceiptItem
                {
                    ProductId = productIds[i],
                    ExpectedQty = quantities[i],
                    ReceivedQty = 0
                });
            }

            _db.GoodsReceipts.Add(grn);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentUserId(), "CREATE_GRN", "Inbound", $"สร้างใบรับสินค้า {grn.GRNNumber} (PO: {poNumber})");

            return RedirectToAction("Receive", new { id = grn.GRNId });
        }

        public async Task<IActionResult> Receive(int id)
        {
            var grn = await _db.GoodsReceipts
                .Include(g => g.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Category)
                .Include(g => g.Items).ThenInclude(i => i.PutAwayLocation)
                .FirstOrDefaultAsync(g => g.GRNId == id);

            if (grn == null) return NotFound();

            ViewBag.Locations = await _db.Locations.Include(l => l.Zone).OrderBy(l => l.BinCode).ToListAsync();
            return View(grn);
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveItem(int grnId, int grnItemId, int receivedQty)
        {
            var item = await _db.GoodsReceiptItems
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.GRNItemId == grnItemId);

            if (item == null) return NotFound();

            item.ReceivedQty = receivedQty;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentUserId(), "RECEIVE_ITEM", "Inbound", $"รับสินค้า {item.Product!.SKU} จำนวน {receivedQty} ชิ้น");

            return RedirectToAction("Receive", new { id = grnId });
        }

        [HttpPost]
        public async Task<IActionResult> PutAway(int grnId, int grnItemId, int locationId)
        {
            var item = await _db.GoodsReceiptItems
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.GRNItemId == grnItemId);

            if (item == null) return NotFound();

            var result = await _inventory.PutAwayAsync(item.ProductId, locationId, item.ReceivedQty, GetCurrentUserId());

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction("Receive", new { id = grnId });
            }

            item.PutAwayLocationId = locationId;
            item.IsPutAway = true;

            // Check if all items are put away
            var grn = await _db.GoodsReceipts.Include(g => g.Items).FirstOrDefaultAsync(g => g.GRNId == grnId);
            if (grn != null && grn.Items.All(i => i.IsPutAway))
            {
                grn.Status = GRNStatus.Completed;
            }
            else if (grn != null)
            {
                grn.Status = GRNStatus.PartiallyReceived;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = result.Message;
            return RedirectToAction("Receive", new { id = grnId });
        }

        [HttpGet]
        public async Task<IActionResult> GetSuggestedLocations(int productId)
        {
            var locations = await _inventory.GetSuggestedLocationsAsync(productId);
            return Json(locations.Select(l => new { l.LocationId, l.BinCode, ZoneName = l.Zone!.ZoneName }));
        }

        private int GetCurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 1;
    }
}
