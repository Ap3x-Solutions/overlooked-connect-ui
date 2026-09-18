using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

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
}