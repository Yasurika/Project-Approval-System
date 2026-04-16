using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectApprovalSystem.Core.Entities;

namespace ProjectApprovalSystem.Web.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly HashSet<string> _allowedRoles;

    public RequireRoleAttribute(params UserRole[] roles)
    {
        _allowedRoles = roles.Select(role => role.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.UserRole);

        if (!userId.HasValue || string.IsNullOrWhiteSpace(role))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return Task.CompletedTask;
        }

        if (!_allowedRoles.Contains(role))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }

        return Task.CompletedTask;
    }
}
