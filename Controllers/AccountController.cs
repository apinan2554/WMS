using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WH_Logistic.Data;
using WH_Logistic.Models;
using WH_Logistic.Services;

namespace WH_Logistic.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;

        public AccountController(ApplicationDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null || user.PasswordHash != password)
            {
                ViewBag.Error = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role.ToString());

            await _audit.LogAsync(user.UserId, "LOGIN", "Account", $"{user.FullName} เข้าสู่ระบบ");
            return RedirectToAction("Index", "Dashboard");
        }

        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            await _audit.LogAsync(userId, "LOGOUT", "Account", "ออกจากระบบ");
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
