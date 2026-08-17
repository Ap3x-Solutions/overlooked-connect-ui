using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;

/*
 This controller handles authentication for the Overlooked Connect Internal Operations Platform:
  - Login (GET):  renders the secure sign-in screen.
  - Login (POST): validates the form and writes the selected demonstration role into session.
  - Logout:       clears the session and returns the user to the sign-in screen.

 In Task 1 the sign-in is simulated. The user chooses which role to demonstrate so that the
 role-based routing described in Section 2.5.2 can be shown across the platform. In Task 2 this
 controller is replaced by Microsoft Entra External ID, with the role read from a role claim and
 multi-factor authentication enforced for the Executive, HR, Safety and Procurement roles
 (Section 8.2). No password is ever stored or compared here.

 AntiForgery tokens are applied to the POST action as a foundational security layer.
 */

namespace OverlookedConnect.Internal.Controllers
{
    public class AccountController : Controller
    {
        /* GET: Account/Login [Smith & Addie, [s.a.]] */
        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            return View(new LoginModel());
        }

        /* POST: Account/Login [Hasan & Anderson, [s.a.]] */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            /* For demonstration purposes the credentials are not checked. The selected role is
               stored in session so that the sidebar and permissions reflect it. */
            if (!DemoData.Users.TryGetValue(model.DemoRole, out var user))
            {
                ModelState.AddModelError(nameof(model.DemoRole), "Unknown role selected.");
                return View(model);
            }

            HttpContext.Session.SetString("RoleKey", model.DemoRole);      /* [Anderson & Smith, [s.a.]] */
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserInitials", user.Initials);
            HttpContext.Session.SetString("UserRole", user.Role);

            /* Employees are routed to the staff application rather than the back office. */
            if (model.DemoRole == "employee")
            {
                return RedirectToAction("Index", "Staff");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        /* GET: Account/Logout */
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been signed out securely.";
            return RedirectToAction("Login");
        }

        /* GET: Account/Error */
        public IActionResult Error() => View("~/Views/Shared/Error.cshtml", new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}

/*
    Reference List:
        - Anderson, R. and Smith, S. [s.a.]. Session and state management in ASP.NET Core | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state> [Accessed 14 August 2026].
        - Hasan, F. and Anderson, R. [s.a.]. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Overview of Microsoft Entra External ID. [online]. Available at: <https://learn.microsoft.com/en-us/entra/external-id/> [Accessed 14 August 2026].
        - Smith, S. and Addie, S. [s.a.]. Handle requests with controllers in ASP.NET Core MVC | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions> [Accessed 14 August 2026].
*/
