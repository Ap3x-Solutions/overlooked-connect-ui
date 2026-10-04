using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

/*
 * Authentication and session handling for the Internal Web application.
 *
 * ASP.NET Core session state is used to retain authenticated user context
 * between requests after the shared API has successfully authenticated the
 * user (Microsoft, [s.a.]a).
 *
 * POST login requests use ASP.NET Core anti-request-forgery validation to
 * protect state-changing form submissions against cross-site request forgery
 * attacks (Microsoft, [s.a.]b).
 *
 * The PublicWeb-to-InternalWeb sign-in handoff does not trust the supplied
 * JWT directly. The token is validated against the shared API before an
 * InternalWeb session is established.
 *
 * References:
 * Microsoft. [s.a.]a. Session and state management in ASP.NET Core.
 * [online]. Available at:
 * <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state>
 * [Accessed 4 October 2026].
 *
 * Microsoft. [s.a.]b. Prevent Cross-Site Request Forgery (XSRF/CSRF)
 * attacks in ASP.NET Core.
 * [online]. Available at:
 * <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery>
 * [Accessed 4 October 2026].
 */

namespace OverlookedConnect.Internal.Controllers;

public sealed class AccountController : Controller
{
    private readonly OverlookedApiClient _apiClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        OverlookedApiClient apiClient,
        ILogger<AccountController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // ------------------------------------------------------------
    // LOGIN - GET
    // ------------------------------------------------------------

    [HttpGet]
    public IActionResult Login()
    {
        /*
         * If a user explicitly navigates back to the login page,
         * clear the previous application session.
         */
        HttpContext.Session.Clear();

        return View(new LoginModel());
    }


    // ------------------------------------------------------------
    // LOGIN - POST
    // ------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _apiClient.LoginAsync(
            model.Email,
            model.Password,
            cancellationToken);


        if (!result.IsSuccess ||
    result.Response is null)
{
    switch (result.Status)
    {
        case ApiLoginStatus.InvalidCredentials:

            ModelState.AddModelError(
                string.Empty,
                "Invalid email address or password.");

            break;

        case ApiLoginStatus.ApiUnavailable:

            ModelState.AddModelError(
                string.Empty,
                "The Overlooked Connect service is currently unavailable. Please try again.");

            break;

        default:

            ModelState.AddModelError(
                string.Empty,
                "We could not complete your sign-in. Please try again.");

            break;
    }

    // Do not retain the submitted password after a failed login.
    model.Password = string.Empty;

    return View(model);
}

        var login = result.Response;


        // --------------------------------------------------------
        // SERVER-SIDE SESSION
        // --------------------------------------------------------

        HttpContext.Session.SetString(
            "AccessToken",
            login.AccessToken);

        HttpContext.Session.SetString(
            "TokenExpiresAtUtc",
            login.ExpiresAtUtc.ToString("O"));

        HttpContext.Session.SetInt32(
            "UserId",
            login.UserId);

        HttpContext.Session.SetString(
            "UserName",
            login.FullName);

        HttpContext.Session.SetString(
            "UserEmail",
            login.Email);

        HttpContext.Session.SetString(
            "UserRole",
            login.Role);

        HttpContext.Session.SetString(
            "RoleKey",
            GetRoleKey(login.Role));

        HttpContext.Session.SetString(
            "UserInitials",
            GetInitials(login.FullName));


        if (!string.IsNullOrWhiteSpace(
                login.EmployeeNumber))
        {
            HttpContext.Session.SetString(
                "EmployeeNumber",
                login.EmployeeNumber);
        }


        if (login.EmployeeId.HasValue)
        {
            HttpContext.Session.SetInt32(
                "EmployeeId",
                login.EmployeeId.Value);
        }


        _logger.LogInformation(
            "User {UserId} signed in to Internal Web with role {Role}.",
            login.UserId,
            login.Role);


        // --------------------------------------------------------
        // ROLE-BASED ROUTING
        // --------------------------------------------------------

        if (string.Equals(
                login.Role,
                "Employee",
                StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(
                "Index",
                "Staff");
        }


        return RedirectToAction(
            "Index",
            "Dashboard");
    }

// =========================================================
// PUBLIC WEB -> INTERNAL WEB SSO
// =========================================================

/// <summary>
/// Receives a JWT from the PublicWeb staff sign-in handoff.
///
/// The token is never trusted directly. It is revalidated against
/// GET /api/auth/me before an InternalWeb session is created.
/// </summary>
[HttpPost]
[IgnoreAntiforgeryToken]
public async Task<IActionResult> Sso(
    string token,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(token))
    {
        TempData["ErrorMessage"] =
            "Sign-in could not be completed. Please sign in again.";

        return RedirectToAction(nameof(Login));
    }

    ApiIdentity? identity;

    try
    {
       identity =
    await _apiClient.ValidateTokenAsync(
        token,
        cancellationToken);
    }
    catch (HttpRequestException)
    {
        TempData["ErrorMessage"] =
            "The sign-in service is currently unavailable. Please try again.";

        return RedirectToAction(nameof(Login));
    }

    if (identity is null ||
        string.IsNullOrWhiteSpace(identity.Role))
    {
        TempData["ErrorMessage"] =
            "That sign-in session is no longer valid. Please sign in again.";

        return RedirectToAction(nameof(Login));
    }

    var fullName =
        string.IsNullOrWhiteSpace(identity.Name)
            ? "Overlooked Connect User"
            : identity.Name;

    EstablishSsoSession(
        token,
        fullName,
        identity.Role,
        identity.EmployeeNumber);

    TempData["SuccessMessage"] =
        $"Signed in from the public website as {fullName}.";

    return LandingForSso(identity.Role);
}

    // ------------------------------------------------------------
    // LOGOUT
    // ------------------------------------------------------------

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        TempData["SuccessMessage"] =
            "You have been signed out securely.";

        return RedirectToAction(
            nameof(Login));
    }


    // ------------------------------------------------------------
    // ERROR
    // ------------------------------------------------------------

    [HttpGet]
    public IActionResult Error()
    {
        return View(
            "~/Views/Shared/Error.cshtml",
            new ErrorViewModel
            {
                RequestId =
                    HttpContext.TraceIdentifier
            });
    }


    // ------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------

    private static string GetRoleKey(
        string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "executive" => "executive",
            "hr" => "hr",
            "safety" => "safety",
            "procurement" => "procurement",
            "employee" => "employee",
            "supplier" => "supplier",

            _ => role.Trim().ToLowerInvariant()
        };
    }


    private static string GetInitials(
        string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "U";
        }

        var names = fullName
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (names.Length == 0)
        {
            return "U";
        }

        if (names.Length == 1)
        {
            return names[0][0]
                .ToString()
                .ToUpperInvariant();
        }

        return string.Concat(
                names[0][0],
                names[^1][0])
            .ToUpperInvariant();
    }
    /// <summary>
/// Establishes the InternalWeb session after the API has validated
/// the JWT received from PublicWeb.
/// </summary>
private void EstablishSsoSession(
    string accessToken,
    string fullName,
    string role,
    string? employeeNumber)
{
    var initials =
        string.Concat(
            fullName
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Where(part => part.Length > 0)
                .Select(part => part[0]))
            .ToUpperInvariant();

    HttpContext.Session.SetString(
        "AccessToken",
        accessToken);

    HttpContext.Session.SetString(
        "UserName",
        fullName);

    HttpContext.Session.SetString(
        "UserInitials",
        string.IsNullOrWhiteSpace(initials)
            ? "OC"
            : initials);

    HttpContext.Session.SetString(
        "UserRole",
        role);

    HttpContext.Session.SetString(
        "RoleKey",
        role.ToLowerInvariant());

    if (!string.IsNullOrWhiteSpace(employeeNumber))
    {
        HttpContext.Session.SetString(
            "EmployeeNumber",
            employeeNumber);
    }
}

/// <summary>
/// Sends employees to the employee portal and other staff
/// roles to the internal dashboard.
/// </summary>
private IActionResult LandingForSso(string role)
{
    return string.Equals(
        role,
        "Employee",
        StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction(
                "Index",
                "Staff")
            : RedirectToAction(
                "Index",
                "Dashboard");
}
}
