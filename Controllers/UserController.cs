using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Filters;
using WH_Logistic.Models;
using WH_Logistic.Services;

namespace WH_Logistic.Controllers
{
    [AuthorizeRole(UserRole.Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;

        public UserController(ApplicationDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.OrderBy(u => u.Username).ToListAsync();
            return View(users);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AppUser user)
        {
            if (await _db.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "ชื่อผู้ใช้นี้มีอยู่แล้ว");
            }

            if (!ModelState.IsValid) return View(user);

            user.IsActive = true;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            await _audit.LogAsync(currentUserId, "CREATE_USER", "UserManagement", $"สร้างผู้ใช้ {user.Username} ({user.Role})");

            TempData["Success"] = $"สร้างผู้ใช้ \"{user.FullName}\" สำเร็จ";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string fullName, UserRole role, bool isActive, string? newPassword)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.Role = role;
            user.IsActive = isActive;

            if (!string.IsNullOrWhiteSpace(newPassword))
                user.PasswordHash = newPassword;

            await _db.SaveChangesAsync();

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            await _audit.LogAsync(currentUserId, "UPDATE_USER", "UserManagement", $"แก้ไขผู้ใช้ {user.Username}");

            TempData["Success"] = $"แก้ไขผู้ใช้ \"{user.FullName}\" สำเร็จ";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            await _audit.LogAsync(currentUserId, "TOGGLE_USER", "UserManagement", $"{(user.IsActive ? "เปิด" : "ปิด")}การใช้งาน {user.Username}");

            TempData["Success"] = $"{(user.IsActive ? "เปิด" : "ปิด")}การใช้งานผู้ใช้ \"{user.FullName}\" สำเร็จ";
            return RedirectToAction("Index");
        }
    }
}
