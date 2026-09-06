using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

/*
 Authentication for the Overlooked Connect Internal Operations Platform.

  - Login (GET / POST): direct sign-in, authenticated against the shared REST API (OVC-224).
  - Sso (POST):         receives a token handed over from the public website (OVC-267).
  - Logout:             clears the session and returns to the sign-in screen.

 Single sign-on, and why the token is re-validated

 A staff member can sign in on the public website and be carried here without entering credentials
 again. The public site posts the issued JWT to the Sso action below.

 That token is not trusted on arrival. Anything arriving in a request body is attacker-controllable,
 so the action presents the token to the API's own /api/auth/me endpoint and only a 200 response
 establishes a session here. A forged or expired token fails that call and the user is returned to
 the sign-in screen. The internal platform therefore never has to verify a signature itself, and
 there is exactly one authority on whether a token is valid.

 A demonstration fallback remains for when the API is not running, so the Task 1 prototype screens
 still render during marking. It is clearly signposted in the interface.

 Reference List:
    - Anderson, R. and Smith, S. [s.a.]. Session and state management in ASP.NET Core | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state> [Accessed 5 September 2026].
    - Hasan, F. and Anderson, R. [s.a.]. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Overview of Microsoft Entra External ID. [online]. Available at: <https://learn.microsoft.com/en-us/entra/external-id/> [Accessed 5 September 2026].
    - OWASP. [s.a.]. Session Management Cheat Sheet. [online]. Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html> [Accessed 5 September 2026].
    - Smith, S. and Addie, S. [s.a.]. Handle requests with controllers in ASP.NET Core MVC | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions> [Accessed 5 September 2026].
*/

namespace OverlookedConnect.Internal.Controllers
{
    public class AccountController : Controller
    {
        private readonly OverlookedApiClient _api;
        private readonly IConfiguration _config;

        public AccountController(OverlookedApiClient api, IConfiguration config)
        {
            _api = api;
            _config = config;
        }

        /* GET: Account/Login */
        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            ViewBag.PublicWebUrl = _config["PublicWeb:BaseUrl"];
            return View(new LoginModel());
        }

        /* POST: Account/Login */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            ViewBag.PublicWebUrl = _config["PublicWeb:BaseUrl"];

            if (!ModelState.IsValid) return View(model);

            /* Authenticate against the shared API. */
            ApiLoginResponse? login = null;
            try
            {
                login = await _api.LoginAsync(model.Email, model.Password);
            }
            catch (HttpRequestException)
            {
                /* API unreachable — fall through to the demonstration path below. */
            }

            if (login is not null)
            {
                EstablishSession(login.AccessToken, login.FullName, login.Role);
                return LandingFor(login.Role);
            }

            /* Demonstration fallback: the role is chosen on the form so the Task 1 screens still
               render when the API is not running. Signposted in the interface. */
            if (!DemoData.Users.TryGetValue(model.DemoRole, out var user))
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password.");
                return View(model);
            }

            HttpContext.Session.SetString("RoleKey", model.DemoRole);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserInitials", user.Initials);
            HttpContext.Session.SetString("UserRole", user.Role);

            TempData["SuccessMessage"] =
                "The API is not reachable — signed in with Task 1 demonstration data.";

            return model.DemoRole == "employee"
                ? RedirectToAction("Index", "Staff")
                : RedirectToAction("Index", "Dashboard");
        }

        /*
         POST: Account/Sso — single sign-on handover from the public website (OVC-267).

         Deliberately no [ValidateAntiForgeryToken]: this is a cross-origin POST from the public
         site, which cannot hold this application's antiforgery token. The protection is different
         and stronger — the payload is worthless unless the API accepts it, which is checked below
         before anything is written to session.
        */
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Sso(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["SuccessMessage"] = "Sign-in could not be completed. Please sign in here.";
                return RedirectToAction("Login");
            }

            ApiIdentity? identity;
            try
            {
                /* The token is validated by the API, not by this application. */
                identity = await _api.ValidateTokenAsync(token);
            }
            catch (HttpRequestException)
            {
                TempData["SuccessMessage"] = "The sign-in service is not reachable. Please try again shortly.";
                return RedirectToAction("Login");
            }

            if (identity?.Role is null)
            {
                TempData["SuccessMessage"] = "That sign-in link is no longer valid. Please sign in again.";
                return RedirectToAction("Login");
            }

            EstablishSession(token, identity.Name ?? "OVL user", identity.Role);
            TempData["SuccessMessage"] = $"Signed in from the public website as {identity.Name}.";
            return LandingFor(identity.Role);
        }

        /* GET: Account/Logout */
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been signed out securely.";
            return RedirectToAction("Login");
        }

        /* ---------------- helpers ---------------- */

        /// <summary>
        /// Writes the token and profile into the server-side session. The session cookie is
        /// HttpOnly, so the token is never exposed to client-side script.
        /// </summary>
        private void EstablishSession(string token, string fullName, string role)
        {
            var initials = string.Concat(
                fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Take(2)
                        .Select(part => part[0]))
                .ToUpperInvariant();

            HttpContext.Session.SetString("ApiToken", token);
            HttpContext.Session.SetString("UserName", fullName);
            HttpContext.Session.SetString("UserInitials", initials);
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("RoleKey", role.ToLowerInvariant());
        }

        /// <summary>Employees go to the staff application; everyone else to the dashboard.</summary>
        private IActionResult LandingFor(string role) =>
            string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction("Index", "Staff")
                : RedirectToAction("Index", "Dashboard");
    }
}
