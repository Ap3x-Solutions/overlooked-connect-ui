using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.PublicWeb.Models;
using OverlookedConnect.PublicWeb.Services;

/*
 Staff access from the public website (OVC-267).

 Requirement: a staff member arriving at the public site can sign in there and be carried into the
 internal operations platform without entering their credentials a second time. One account, one
 password, one audit identity across all three clients — the single sign-on principle in
 Section 1.3 and Section 8.2 of the Task 1 documentation.

 How the handoff works:

   1. Staff submits credentials here. This site calls POST /api/auth/login on the shared API.
   2. The API returns a signed JWT carrying the user's role.
   3. A supplier signing in stays here — their portal is the public status tracker, not the back
      office. Everyone else is handed over.
   4. The handoff view auto-submits a POST form to the internal platform's /Account/Sso endpoint,
      carrying the token in the request body.
   5. The internal platform does not trust the token on arrival. It calls GET /api/auth/me with it,
      and only a 200 from the API establishes the internal session.

 Why POST and not a query string: a token in a URL is written to browser history, server access
 logs and any Referer header sent to a third party. In the body it is written to none of those.
 This is the same reasoning that keeps the token out of localStorage on the internal side.

 Reference List:
    - Microsoft Learn. [s.a.]. Handle requests with controllers in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery> [Accessed 5 September 2026].
    - OWASP. [s.a.]. Session Management Cheat Sheet. [online]. Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html> [Accessed 5 September 2026].
    - Internet Engineering Task Force. 2015. RFC 7519: JSON Web Token (JWT). [online]. Available at: <https://www.rfc-editor.org/rfc/rfc7519> [Accessed 5 September 2026].
*/

namespace OverlookedConnect.PublicWeb.Controllers
{
    public class StaffAccessController : Controller
    {
        private readonly OverlookedApiClient _api;
        private readonly IConfiguration _config;

        public StaffAccessController(OverlookedApiClient api, IConfiguration config)
        {
            _api = api;
            _config = config;
        }

        /* GET: StaffAccess/Login */
        [HttpGet]
        public IActionResult Login() => View(new StaffLoginModel());

        /* POST: StaffAccess/Login */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(StaffLoginModel model)
        {
            if (!ModelState.IsValid) return View(model);

            ApiLoginResponse? login;
            try
            {
                login = await _api.LoginAsync(model.Email, model.Password);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty,
                    "The sign-in service is not reachable at the moment. Please try again shortly.");
                return View(model);
            }

            /* One message for an unknown email and for a wrong password, so this page cannot be
               used to determine which addresses are registered. */
            if (login is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password.");
                return View(model);
            }

            /* A supplier belongs on the public status tracker, not in the back office. */
            if (string.Equals(login.Role, "Supplier", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.SetString("SupplierToken", login.AccessToken);
                HttpContext.Session.SetString("SupplierName", login.FullName);
                TempData["SuccessMessage"] = $"Welcome back, {login.FullName}.";
                return RedirectToAction("SupplierStatus", "Home");
            }

            /* Everyone else is handed to the internal platform. */
            var internalBase = _config["InternalWeb:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:7244";

            return View("Handoff", new StaffHandoffModel
            {
                PostUrl = $"{internalBase}/Account/Sso",
                AccessToken = login.AccessToken,
                FullName = login.FullName,
                Role = login.Role
            });
        }
    }
}
