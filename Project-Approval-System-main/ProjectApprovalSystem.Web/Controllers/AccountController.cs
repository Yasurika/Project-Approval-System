using Microsoft.AspNetCore.Mvc;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Web.Infrastructure;
using ProjectApprovalSystem.Web.Models;

namespace ProjectApprovalSystem.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var user = await _userService.AuthenticateAsync(model.Email, model.Password);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Invalid email or password.";
            return View(model);
        }

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.Id);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.Role.ToString());
        HttpContext.Session.SetString(SessionKeys.UserName, user.FullName);
        HttpContext.Session.SetString(SessionKeys.UserEmail, user.Email);

        return RedirectToRoleDashboard(user.Role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToRoleDashboard(UserRole role)
    {
        return role switch
        {
            UserRole.Student => RedirectToAction("Dashboard", "Student"),
            UserRole.Supervisor => RedirectToAction("Dashboard", "Supervisor"),
            UserRole.ModuleLeader or UserRole.Administrator => RedirectToAction("Dashboard", "Admin"),
            _ => RedirectToAction(nameof(Login))
        };
    }
}
