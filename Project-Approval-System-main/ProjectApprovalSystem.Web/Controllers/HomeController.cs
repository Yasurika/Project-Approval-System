using Microsoft.AspNetCore.Mvc;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Web.Infrastructure;

namespace ProjectApprovalSystem.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        var role = HttpContext.Session.GetString(SessionKeys.UserRole);
        if (!userId.HasValue || string.IsNullOrWhiteSpace(role))
        {
            return RedirectToAction("Login", "Account");
        }

        return role switch
        {
            nameof(UserRole.Student) => RedirectToAction("Dashboard", "Student"),
            nameof(UserRole.Supervisor) => RedirectToAction("Dashboard", "Supervisor"),
            nameof(UserRole.ModuleLeader) => RedirectToAction("Dashboard", "Admin"),
            nameof(UserRole.Administrator) => RedirectToAction("Dashboard", "Admin"),
            _ => RedirectToAction("Login", "Account")
        };
    }
}