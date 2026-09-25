using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

/*
 This controller serves the Executive-facing screens of the Internal Operations Platform:
  - Index:     the Management Dashboard, aggregating workforce, safety, supplier and CSR indicators
               into a single live view (FR-01).
  - Approvals: the consolidated approvals queue, showing leave, supplier and CSR spend items awaiting
               the signed-in user's decision (FR-01, US-02).
  - Approve / Decline: POST actions that simulate an approval decision.

 Every approval or decline in the live system writes an immutable AuditLog entry capturing the acting
 user, timestamp, action, entity and the values before and after the change, which is how NFR-08 is
 enforced structurally rather than by convention. In Task 1 that write is simulated by the
 confirmation message returned to the user.
 */

namespace OverlookedConnect.Internal.Controllers
{
    /*
     (OVC-102): DashboardController.Index was modified to fetch the most recent Incidents from the backend API via OverlookedApiClient.
     The result is mapped to the existing DemoData Incident view model and the controller falls back to DemoData when the API cannot be reached.
    */
    public class DashboardController : Controller
    {
        private readonly OverlookedApiClient _api;

        public DashboardController(OverlookedApiClient api)
        {
            _api = api;
        }
        /* GET: Dashboard/Index */
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            List<Incident> recent;
            var live = true;

            var accessToken = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction("Login", "Account");
            }

            try
            {
                var apiItems = await _api.GetIncidentsAsync(null, accessToken, cancellationToken);

                recent = apiItems
                    .Select(ai => new Incident
                    {
                        IncidentId = string.IsNullOrWhiteSpace(ai.Reference) ? $"INC-{ai.IncidentId}" : ai.Reference,
                        Title = ai.IncidentType,
                        Site = ai.Site,
                        Severity = ai.Severity,
                        InvestigationStatus = ai.Status,
                        ReportedBy = string.IsNullOrWhiteSpace(ai.EmployeeName) && string.IsNullOrWhiteSpace(ai.EmployeeNumber)
                            ? ""
                            : $"{ai.EmployeeName} ({ai.EmployeeNumber})",
                        DateReported = ai.DateReported.ToString("dd MMM yyyy, HH:mm"),
                        Description = ai.Description,
                        PhotoCount = 0,
                        LifecycleStage = ai.Status switch
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
                    })
                    .Take(4)
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction("Login", "Account");
            }
            catch (HttpRequestException)
            {
                live = false;
                recent = DemoData.Incidents.Take(4).ToList();

                TempData["SuccessMessage"] ??= "The API is not reachable — showing Task 1 demonstration data.";
            }
            catch (Exception)
            {
                live = false;
                recent = DemoData.Incidents.Take(4).ToList();

                TempData["SuccessMessage"] ??= "Live incident information is currently unavailable — showing Task 1 demonstration data.";
            }

            var model = new DashboardViewModel
            {
                Approvals = DemoData.Approvals,
                RecentIncidents = recent,
                Production = DemoData.ProductionByMonth,
                Workforce = DemoData.WorkforceByUnit
            };

            ViewBag.LiveData = live;

            return View(model);
        }

        /* GET: Dashboard/Approvals */
        public IActionResult Approvals() => View(DemoData.Approvals);

        /* POST: Dashboard/Approve */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(string requester, string type)
        {
            /* For demonstration/simulation purposes, it will just show a success message in the prototype. */
            TempData["SuccessMessage"] =
                $"{type} request from {requester} approved. An audit entry has been written recording the acting user, timestamp and the value before and after the change.";
            return RedirectToAction("Approvals");
        }

        /* POST: Dashboard/Decline */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decline(string requester, string type)
        {
            TempData["SuccessMessage"] =
                $"{type} request from {requester} declined. The requester has been notified and an audit entry recorded.";
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
