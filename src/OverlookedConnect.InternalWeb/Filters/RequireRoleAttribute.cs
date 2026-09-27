using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OverlookedConnect.Internal.Filters;

/*
 OVC-98 — server-side role gate.

 The nav menu in _Layout.cshtml hides items a user's role shouldn't see, but
 that is only a UX nicety: a user can still type the URL directly. This filter
 is the actual access boundary, applied to controller classes.

 Reference List:
    - Microsoft Learn. [s.a.]. Filters in ASP.NET Core. [online].
      Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters>
      [Accessed 27 September 2026].
*/
public sealed class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string[] _roleKeys;

    public RequireRoleAttribute(params string[] roleKeys) => _roleKeys = roleKeys;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var roleKey = context.HttpContext.Session.GetString("RoleKey");

        if (roleKey is null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (!_roleKeys.Contains(roleKey))
        {
            context.Result = new ForbidResult();
        }
    }
}