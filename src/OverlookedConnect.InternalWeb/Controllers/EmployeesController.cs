using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

/*
 This controller serves the Employee Management module (FR-02, US-03), used by HR & Operations.
 It renders the employee register from GET /api/employees, which is protected by
 [Authorize(Roles = "HR,Executive")] on the API side; a valid login with a different role
 receives 403 and is shown an access-denied message rather than the register.

 Search, business unit and status filters are forwarded to the API. Filtering and pagination
 are performed server-side by EmployeeService.SearchAsync — the UI does not re-filter.

 Reference List:
    - Microsoft Learn. [s.a.]. ASP.NET Core MVC controllers. [online].
      Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions>
      [Accessed 14 August 2026].
    - Microsoft Learn. [s.a.]. ViewBag, ViewData and TempData in ASP.NET Core. [online].
      Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview>
      [Accessed 14 August 2026].
*/

namespace OverlookedConnect.Internal.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly OverlookedApiClient _api;

        public EmployeesController(OverlookedApiClient api) => _api = api;

        /* GET: Employees/Index */
        public async Task<IActionResult> Index(
            string? businessUnit,
            string? status,
            string? search,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            var accessToken = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] = "Your session has expired. Please sign in again.";
                return RedirectToAction("Login", "Account");
            }

            var businessUnitFilter = businessUnit is null or "" or "All units" ? null : businessUnit;
            var statusFilter = status is null or "" or "All" ? null : status;

            var result = await _api.GetEmployeesAsync(
                search, businessUnitFilter, statusFilter, page, pageSize: 20,
                accessToken, cancellationToken);

            var model = new EmployeeRegisterViewModel
            {
                Search = search,
                BusinessUnit = businessUnit,
                Status = status
            };

            switch (result.Status)
            {
                case ApiEmployeeStatus.Unauthorized:
                    TempData["ErrorMessage"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Login", "Account");

                case ApiEmployeeStatus.Forbidden:
                    TempData["ErrorMessage"] = "You do not have permission to view the employee register.";
                    break;

                case ApiEmployeeStatus.ApiUnavailable:
                    TempData["ErrorMessage"] = "The employee register is currently unavailable.";
                    break;

                case ApiEmployeeStatus.ApiFailure:
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Employees could not be loaded.";
                    break;

                default:
                    model.Employees = result.Data?.Items ?? new();
                    model.Total = result.Data?.Total ?? 0;
                    break;
            }

            ViewBag.BusinessUnit = businessUnit ?? "All units";
            ViewBag.Status = status ?? "All";
            ViewBag.Search = search;
            ViewBag.Total = model.Total;

            return View(model);
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. ASP.NET Core MVC controllers. [online].
          Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions>
          [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. ViewBag, ViewData and TempData in ASP.NET Core. [online].
          Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview>
          [Accessed 14 August 2026].
*/
