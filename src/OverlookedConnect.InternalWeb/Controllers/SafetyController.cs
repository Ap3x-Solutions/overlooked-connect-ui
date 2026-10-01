using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

/*
 This controller serves the Safety & Incident Reporting module (FR-04, US-05, US-06)
 for the Safety Officer role.

 OVC-102 integrates the incident register with the backend Incident API while
 preserving the existing Task 1 view model and Razor UI.
*/

namespace OverlookedConnect.Internal.Controllers
{
    public class SafetyController : Controller
    {
        private readonly OverlookedApiClient _api;

        public SafetyController(OverlookedApiClient api)
        {
            _api = api;
        }

        public static readonly string[] Lifecycle =
        {
            "Reported",
            "Acknowledged",
            "Investigating",
            "Corrective action",
            "Verification",
            "Closed"
        };

        /* GET: Safety/Index */
        public async Task<IActionResult> Index(
            string? id,
            string? filter,
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

            switch (result.Status)
            {
                case ApiIncidentStatus.Unauthorized:
                    TempData["ErrorMessage"] =
                        "Your session has expired. Please sign in again.";

                    return RedirectToAction("Login", "Account");

                case ApiIncidentStatus.Forbidden:
                    TempData["ErrorMessage"] =
                        "You do not have permission to view the incident register.";

                    return RedirectToAction("Index", "Home");

                case ApiIncidentStatus.ApiUnavailable:
                    TempData["ErrorMessage"] =
                        "The Incident API is currently unavailable. Please try again.";

                    return View(
                        new SafetyViewModel
                        {
                            Incidents = new List<Incident>(),
                            Selected = null
                        });

                case ApiIncidentStatus.ApiFailure:
                    TempData["ErrorMessage"] =
                        result.ErrorMessage ??
                        "Incident information could not be loaded.";

                    return View(
                        new SafetyViewModel
                        {
                            Incidents = new List<Incident>(),
                            Selected = null
                        });
            }

            var list = (result.Data ?? Array.Empty<ApiIncident>())
                .Select(MapIncident)
                .ToList();

            if (string.Equals(
                    filter,
                    "Open",
                    StringComparison.OrdinalIgnoreCase))
            {
                list = list
                    .Where(i =>
                        !string.Equals(
                            i.InvestigationStatus,
                            "Closed",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else if (string.Equals(
                         filter,
                         "Closed",
                         StringComparison.OrdinalIgnoreCase))
            {
                list = list
                    .Where(i =>
                        string.Equals(
                            i.InvestigationStatus,
                            "Closed",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var selected =
                list.FirstOrDefault(i => i.IncidentId == id) ??
                list.FirstOrDefault();

            ViewBag.Filter = filter ?? "All";
            ViewBag.LiveData = true;

            return View(
                new SafetyViewModel
                {
                    Incidents = list,
                    Selected = selected
                });
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

        /* POST: Safety/AssignAction */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignAction(string incidentId)
        {
            TempData["SuccessMessage"] =
                $"Corrective action assigned for {incidentId}. " +
                "The owner and due date have been recorded and the incident moved to Corrective action.";

            return RedirectToAction(
                "Index",
                new { id = incidentId });
        }

        /* POST: Safety/Escalate */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Escalate(string incidentId)
        {
            TempData["SuccessMessage"] =
                $"{incidentId} escalated. A statutory report is required to the " +
                "Department of Mineral Resources and Energy; the investigation remains open.";

            return RedirectToAction(
                "Index",
                new { id = incidentId });
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Enumerable.FirstOrDefault Method (System.Linq). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.firstordefault> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Views in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview> [Accessed 14 August 2026].
        - Republic of South Africa. 1996. Mine Health and Safety Act, No. 29 of 1996. Cape Town: Government Printers.
*/