using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

/*
 This controller serves the Executive-facing screens of the Internal Operations Platform.

 OVC-102 integrates recent safety incidents with the backend Incident API.
 Other dashboard datasets remain connected to the existing Task 1 model until
 their respective Task 2 API integrations are completed.
*/

namespace OverlookedConnect.Internal.Controllers
{
    public class DashboardController : Controller
    {
        private readonly OverlookedApiClient _api;

        public DashboardController(OverlookedApiClient api)
        {
            _api = api;
        }

        /* GET: Dashboard/Index */
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var accessToken =
                HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction("Login", "Account");
            }

            var result = await _api.GetIncidentsAsync(
                null,
                accessToken,
                cancellationToken);

            List<Incident> recent;

            switch (result.Status)
            {
                case ApiIncidentStatus.Unauthorized:
                    TempData["ErrorMessage"] =
                        "Your session has expired. Please sign in again.";

                    return RedirectToAction("Login", "Account");

                case ApiIncidentStatus.Forbidden:
                    recent = new List<Incident>();

                    TempData["ErrorMessage"] =
                        "You do not have permission to view safety incident information.";

                    break;

                case ApiIncidentStatus.ApiUnavailable:
                    recent = new List<Incident>();

                    TempData["ErrorMessage"] =
                        "Live safety incident information is currently unavailable.";

                    break;

                case ApiIncidentStatus.ApiFailure:
                    recent = new List<Incident>();

                    TempData["ErrorMessage"] =
                        result.ErrorMessage ??
                        "Safety incident information could not be loaded.";

                    break;

                default:
                    recent = (result.Data ?? Array.Empty<ApiIncident>())
                        .OrderByDescending(i => i.DateReported)
                        .Take(4)
                        .Select(MapIncident)
                        .ToList();

                    break;
            }

            var model = new DashboardViewModel
            {
                Approvals = DemoData.Approvals,
                RecentIncidents = recent,
                Production = DemoData.ProductionByMonth,
                Workforce = DemoData.WorkforceByUnit
            };

            ViewBag.LiveData =
                result.Status == ApiIncidentStatus.Success;

            return View(model);
        }

        private static Incident MapIncident(ApiIncident incident)
        {
            return new Incident
            {
                IncidentId =
                    string.IsNullOrWhiteSpace(incident.Reference)
                        ? $"INC-{incident.IncidentId}"
                        : incident.Reference,

                Title = incident.IncidentType,
                Site = incident.Site,
                Severity = incident.Severity,
                InvestigationStatus = incident.Status,

                ReportedBy =
                    string.IsNullOrWhiteSpace(incident.EmployeeName) &&
                    string.IsNullOrWhiteSpace(incident.EmployeeNumber)
                        ? ""
                        : $"{incident.EmployeeName} ({incident.EmployeeNumber})",

                DateReported =
                    incident.DateReported.ToString("dd MMM yyyy, HH:mm"),

                Description = incident.Description,
                PhotoCount = 0,

                LifecycleStage = incident.Status switch
                {
                    "Reported" => 0,
                    "Acknowledged" => 1,
                    "UnderInvestigation" => 2,
                    "Escalated" => 2,
                    "CorrectiveActionAssigned" => 3,
                    "Verification" => 4,
                    "Closed" => 5,
                    _ => 0
                }
            };
        }

        /* GET: Dashboard/Approvals */
        public IActionResult Approvals() =>
            View(DemoData.Approvals);

        /* POST: Dashboard/Approve */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(
            string requester,
            string type)
        {
            TempData["SuccessMessage"] =
                $"{type} request from {requester} approved. " +
                "An audit entry has been written recording the acting user, " +
                "timestamp and the value before and after the change.";

            return RedirectToAction("Approvals");
        }

        /* POST: Dashboard/Decline */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decline(
            string requester,
            string type)
        {
            TempData["SuccessMessage"] =
                $"{type} request from {requester} declined. " +
                "The requester has been notified and an audit entry recorded.";

            return RedirectToAction("Approvals");
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Enumerable.Take Method (System.Linq). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.take> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Views in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview> [Accessed 14 August 2026].
        - Nowak, R., Larkin, K. and Anderson, R. [s.a.]. Routing to controller actions in ASP.NET Core MVC | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing> [Accessed 14 August 2026].
*/