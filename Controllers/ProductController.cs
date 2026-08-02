using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Filters;
using WH_Logistic.Models;
using WH_Logistic.Services;

namespace WH_Logistic.Controllers
{
    [AuthorizeRole(UserRole.Admin, UserRole.FactoryManager)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;

        public ProductController(ApplicationDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .OrderBy(p => p.SKU)
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.ProductCategories.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductMaster product)
        {
            if (await _db.Products.AnyAsync(p => p.SKU == product.SKU))
            {
                ModelState.AddModelError("SKU", "รหัส SKU นี้มีอยู่แล้วในระบบ");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.ProductCategories.ToListAsync();
                return View(product);
            }

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(1, "CREATE_PRODUCT", "MasterData", $"เพิ่มสินค้า {product.SKU} - {product.ProductName}");

            TempData["Success"] = $"เพิ่มสินค้า \"{product.ProductName}\" สำเร็จ";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = await _db.ProductCategories.ToListAsync();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductMaster product)
        {
            if (id != product.ProductId) return BadRequest();

            var existing = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.SKU == product.SKU && p.ProductId != id);
            if (existing != null)
            {
                ModelState.AddModelError("SKU", "รหัส SKU นี้มีอยู่แล้วในระบบ");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.ProductCategories.ToListAsync();
                return View(product);
            }

            _db.Products.Update(product);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(1, "UPDATE_PRODUCT", "MasterData", $"แก้ไขสินค้า {product.SKU} - {product.ProductName}");

            TempData["Success"] = $"แก้ไขสินค้า \"{product.ProductName}\" สำเร็จ";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Check if product has inventory
            var hasStock = await _db.InventoryBalances.AnyAsync(b => b.ProductId == id && b.Quantity > 0);
            if (hasStock)
            {
                TempData["Error"] = "ไม่สามารถลบสินค้าที่ยังมีสต็อกคงเหลือได้";
                return RedirectToAction("Index");
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(1, "DELETE_PRODUCT", "MasterData", $"ลบสินค้า {product.SKU} - {product.ProductName}");

            TempData["Success"] = $"ลบสินค้า \"{product.ProductName}\" สำเร็จ";
            return RedirectToAction("Index");
        }
    }
}
