using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Filters;
using WH_Logistic.Models;
using WH_Logistic.Services;

namespace WH_Logistic.Controllers
{
    [AuthorizeRole(UserRole.Admin, UserRole.WarehouseStaff, UserRole.TransportStaff, UserRole.OutboundStaff, UserRole.FactoryManager)]
    public class OutboundController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;

        public OutboundController(ApplicationDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        // Sales Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _db.SalesOrders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> CreateOrder()
        {
            ViewBag.Products = await _db.Products.Include(p => p.Category).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(string customerName, int[] productIds, int[] quantities)
        {
            var order = new SalesOrder
            {
                OrderNumber = $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CustomerName = customerName,
                Status = OrderStatus.Pending
            };

            for (int i = 0; i < productIds.Length; i++)
            {
                order.Items.Add(new SalesOrderItem
                {
                    ProductId = productIds[i],
                    Quantity = quantities[i]
                });
            }

            _db.SalesOrders.Add(order);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentUserId(), "CREATE_ORDER", "Outbound", $"สร้างออเดอร์ {order.OrderNumber} ลูกค้า: {customerName}");

            return RedirectToAction("Index");
        }

        // Generate Picking List - optimized by location sort order
        [HttpPost]
        public async Task<IActionResult> GeneratePickingList(int orderId)
        {
            var order = await _db.SalesOrders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            var pickingList = new PickingList
            {
                PickingNumber = $"PL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                OrderId = orderId,
                AssignedToUserId = GetCurrentUserId(),
                Status = PickingStatus.Pending
            };

            int sortOrder = 0;
            foreach (var item in order.Items)
            {
                // Find best locations with stock, sorted by BinCode for optimized picking path
                var balances = await _db.InventoryBalances
                    .Include(b => b.Location)
                    .Where(b => b.ProductId == item.ProductId && b.Quantity > 0)
                    .OrderBy(b => b.Location!.BinCode) // Optimized picking path
                    .ToListAsync();

                int remainingQty = item.Quantity;
                foreach (var balance in balances)
                {
                    if (remainingQty <= 0) break;
                    int pickQty = Math.Min(remainingQty, balance.Quantity);
                    pickingList.Items.Add(new PickingListItem
                    {
                        ProductId = item.ProductId,
                        LocationId = balance.LocationId,
                        RequiredQty = pickQty,
                        SortOrder = ++sortOrder
                    });
                    remainingQty -= pickQty;
                }
            }

            // Re-sort all items by location BinCode for shortest picking path
            var sortedItems = pickingList.Items.OrderBy(i => i.LocationId).ToList();
            for (int i = 0; i < sortedItems.Count; i++)
                sortedItems[i].SortOrder = i + 1;

            order.Status = OrderStatus.PickingInProgress;
            _db.PickingLists.Add(pickingList);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentUserId(), "GENERATE_PICKING", "Outbound", $"สร้าง Picking List {pickingList.PickingNumber} สำหรับออเดอร์ {order.OrderNumber}");

            return RedirectToAction("PickingDetail", new { id = pickingList.PickingId });
        }

        public async Task<IActionResult> PickingDetail(int id)
        {
            var picking = await _db.PickingLists
                .Include(p => p.SalesOrder)
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .Include(p => p.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Zone)
                .FirstOrDefaultAsync(p => p.PickingId == id);

            if (picking == null) return NotFound();
            return View(picking);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPicking(int pickingId, int[] pickingItemIds, int[] pickedQtys)
        {
            var picking = await _db.PickingLists
                .Include(p => p.Items)
                .Include(p => p.SalesOrder).ThenInclude(o => o!.Items)
                .FirstOrDefaultAsync(p => p.PickingId == pickingId);

            if (picking == null) return NotFound();

            for (int i = 0; i < pickingItemIds.Length; i++)
            {
                var item = picking.Items.FirstOrDefault(x => x.PickingItemId == pickingItemIds[i]);
                if (item != null)
                {
                    item.PickedQty = pickedQtys[i];
                }
            }

            picking.Status = PickingStatus.Completed;
            picking.SalesOrder!.Status = OrderStatus.Picked;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentUserId(), "CONFIRM_PICKING", "Outbound", $"ยืนยัน Picking {picking.PickingNumber}");

            return RedirectToAction("Pack", new { id = pickingId });
        }

        public async Task<IActionResult> Pack(int id)
        {
            var picking = await _db.PickingLists
                .Include(p => p.SalesOrder)
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .Include(p => p.Items).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(p => p.PickingId == id);

            if (picking == null) return NotFound();
            return View(picking);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPack(int pickingId)
        {
            var picking = await _db.PickingLists
                .Include(p => p.Items)
                .Include(p => p.SalesOrder)
                .FirstOrDefaultAsync(p => p.PickingId == pickingId);

            if (picking == null) return NotFound();

            // Deduct stock
            foreach (var item in picking.Items)
            {
                var balance = await _db.InventoryBalances
                    .FirstOrDefaultAsync(b => b.ProductId == item.ProductId && b.LocationId == item.LocationId);

                if (balance != null)
                {
                    balance.Quantity -= item.PickedQty;
                    balance.LastUpdated = DateTime.UtcNow;
                }
            }

            picking.Status = PickingStatus.Packed;
            picking.SalesOrder!.Status = OrderStatus.Packed;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(GetCurrentUserId(), "PACK_COMPLETE", "Outbound", $"แพ็คสินค้าเสร็จสิ้น Picking {picking.PickingNumber}, ตัดสต็อกแล้ว");

            TempData["Success"] = "แพ็คสินค้าสำเร็จและตัดสต็อกเรียบร้อยแล้ว!";
            return RedirectToAction("Index");
        }

        private int GetCurrentUserId() => HttpContext.Session.GetInt32("UserId") ?? 1;
    }
}
