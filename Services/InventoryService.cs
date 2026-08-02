using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Models;

namespace WH_Logistic.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;

        public InventoryService(ApplicationDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<(bool Success, string Message)> PutAwayAsync(int productId, int locationId, int quantity, int userId)
        {
            var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return (false, "ไม่พบข้อมูลสินค้า");

            var location = await _db.Locations.Include(l => l.Zone).FirstOrDefaultAsync(l => l.LocationId == locationId);
            if (location == null) return (false, "ไม่พบตำแหน่งจัดเก็บ");

            // Rule: Product category must match zone category
            if (location.Zone!.CategoryId != product.CategoryId)
            {
                return (false, $"ไม่สามารถจัดเก็บได้! สินค้าประเภท \"{product.Category!.CategoryName}\" ต้องจัดเก็บในโซนที่กำหนดสำหรับประเภทนี้เท่านั้น (โซนปัจจุบัน: {location.Zone.ZoneName} รองรับเฉพาะ \"{location.Zone.Category!.CategoryName}\")");
            }

            var balance = await _db.InventoryBalances
                .FirstOrDefaultAsync(b => b.ProductId == productId && b.LocationId == locationId);

            if (balance != null)
            {
                balance.Quantity += quantity;
                balance.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                _db.InventoryBalances.Add(new InventoryBalance
                {
                    ProductId = productId,
                    LocationId = locationId,
                    Quantity = quantity
                });
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(userId, "PUT_AWAY", "Inbound", $"สินค้า {product.SKU} จำนวน {quantity} ชิ้น ไปยัง {location.BinCode}", location.BinCode);

            return (true, $"จัดเก็บสำเร็จ! {product.ProductName} จำนวน {quantity} ชิ้น ที่ตำแหน่ง {location.BinCode}");
        }

        public async Task<(bool Success, string Message)> TransferStockAsync(int productId, int fromLocationId, int toLocationId, int quantity, int userId)
        {
            var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return (false, "ไม่พบข้อมูลสินค้า");

            var toLocation = await _db.Locations.Include(l => l.Zone).ThenInclude(z => z!.Category).FirstOrDefaultAsync(l => l.LocationId == toLocationId);
            if (toLocation == null) return (false, "ไม่พบตำแหน่งปลายทาง");

            // Validate zone category
            if (toLocation.Zone!.CategoryId != product.CategoryId)
            {
                return (false, $"ไม่สามารถย้ายได้! สินค้าประเภท \"{product.Category!.CategoryName}\" ไม่สามารถย้ายไปโซน \"{toLocation.Zone.ZoneName}\" ที่รองรับเฉพาะ \"{toLocation.Zone.Category!.CategoryName}\"");
            }

            var fromBalance = await _db.InventoryBalances
                .FirstOrDefaultAsync(b => b.ProductId == productId && b.LocationId == fromLocationId);

            if (fromBalance == null || fromBalance.Quantity < quantity)
                return (false, "สต็อกไม่เพียงพอสำหรับการย้าย");

            fromBalance.Quantity -= quantity;
            fromBalance.LastUpdated = DateTime.UtcNow;

            var toBalance = await _db.InventoryBalances
                .FirstOrDefaultAsync(b => b.ProductId == productId && b.LocationId == toLocationId);

            if (toBalance != null)
            {
                toBalance.Quantity += quantity;
                toBalance.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                _db.InventoryBalances.Add(new InventoryBalance
                {
                    ProductId = productId,
                    LocationId = toLocationId,
                    Quantity = quantity
                });
            }

            var fromLoc = await _db.Locations.FindAsync(fromLocationId);
            _db.StockTransfers.Add(new StockTransfer
            {
                TransferNumber = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}",
                ProductId = productId,
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                Quantity = quantity,
                TransferByUserId = userId
            });

            await _db.SaveChangesAsync();
            await _audit.LogAsync(userId, "STOCK_TRANSFER", "Inventory", $"ย้าย {product.SKU} จำนวน {quantity} จาก {fromLoc!.BinCode} ไปยัง {toLocation.BinCode}", toLocation.BinCode);

            return (true, $"ย้ายสำเร็จ! {product.ProductName} จำนวน {quantity} ชิ้น จาก {fromLoc.BinCode} ไปยัง {toLocation.BinCode}");
        }

        public async Task<List<InventoryBalance>> GetLowStockItemsAsync()
        {
            return await _db.InventoryBalances
                .Include(b => b.Product)
                .Include(b => b.Location).ThenInclude(l => l!.Zone)
                .GroupBy(b => b.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
                .Join(_db.Products, a => a.ProductId, p => p.ProductId, (a, p) => new { a.TotalQty, Product = p })
                .Where(x => x.TotalQty <= x.Product.MinStock)
                .Select(x => new InventoryBalance { ProductId = x.Product.ProductId, Product = x.Product, Quantity = x.TotalQty })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> AdjustStockAsync(int productId, int locationId, int newQty, int userId, string reason)
        {
            var balance = await _db.InventoryBalances
                .Include(b => b.Product)
                .Include(b => b.Location)
                .FirstOrDefaultAsync(b => b.ProductId == productId && b.LocationId == locationId);

            if (balance == null) return (false, "ไม่พบข้อมูลสต็อกที่ตำแหน่งนี้");

            int oldQty = balance.Quantity;
            balance.Quantity = newQty;
            balance.LastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(userId, "STOCK_ADJUSTMENT", "CycleCount", $"ปรับสต็อก {balance.Product!.SKU} ที่ {balance.Location!.BinCode}: {oldQty} -> {newQty}, เหตุผล: {reason}", balance.Location.BinCode);

            return (true, $"ปรับสต็อกสำเร็จ: {oldQty} → {newQty}");
        }

        public async Task<List<Location>> GetSuggestedLocationsAsync(int productId)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return new List<Location>();

            // Suggest locations in zones matching product category, ordered by BinCode
            return await _db.Locations
                .Include(l => l.Zone)
                .Where(l => l.Zone!.CategoryId == product.CategoryId)
                .OrderBy(l => l.BinCode)
                .ToListAsync();
        }
    }
}
