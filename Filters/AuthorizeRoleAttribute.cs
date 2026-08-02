using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WH_Logistic.Models;

namespace WH_Logistic.Filters
{
    /// <summary>
    /// ตรวจสอบว่าผู้ใช้ล็อกอินแล้ว และมีสิทธิ์ตาม Role ที่กำหนด
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly UserRole[] _roles;

        public AuthorizeRoleAttribute(params UserRole[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // If no specific roles required, just check login
            if (_roles.Length == 0) return;

            var roleStr = context.HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(roleStr) || !Enum.TryParse<UserRole>(roleStr, out var userRole))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (!_roles.Contains(userRole))
            {
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml",
                    StatusCode = 403
                };
            }
        }
    }

    /// <summary>
    /// ตรวจสอบแค่ว่าล็อกอินแล้ว ไม่ตรวจ Role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeLoginAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
