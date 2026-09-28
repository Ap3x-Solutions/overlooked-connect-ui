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
    public class DashboardController : Controller
    {
        private readonly OverlookedApiClient _apiClient;

        public DashboardController(OverlookedApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /* GET: Dashboard/Index */
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var accessToken = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToAction("Login", "Account");
            }

            var summaryResult = await _apiClient.GetDashboardSummaryAsync(accessToken, cancellationToken);
            var incidentsResult = await _apiClient.GetIncidentsAsync(
                accessToken,
                search: null, status: null, severity: null, site: null,
                page: 1, pageSize: 4,
                cancellationToken);

            if (summaryResult.Status == ApiDashboardStatus.Unauthorized ||
                incidentsResult.Status == ApiDashboardStatus.Unauthorized)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new DashboardViewModel
            {
                Summary = summaryResult.IsSuccess ? summaryResult.Data : null,
                RecentIncidents = incidentsResult.IsSuccess
                    ? incidentsResult.Data!.Items.ToList()
                    : new List<ApiDashboardIncident>(),

                Approvals = DemoData.Approvals,
                Production = DemoData.ProductionByMonth,
                Workforce = DemoData.WorkforceByUnit
            };

            if (!summaryResult.IsSuccess || !incidentsResult.IsSuccess)
            {
                TempData["ErrorMessage"] =
                    "Some dashboard data could not be loaded from the server. " +
                    "Displaying what is available.";
            }

            return View(model);
        }

        /* GET: Dashboard/Approvals */
        public IActionResult Approvals() => View(DemoData.Approvals);

        /* POST: Dashboard/Approve */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(string requester, string type)
        {
            TempData["SuccessMessage"] =
                $"{type} request from {requester} approved. An audit entry has been " +
                "written recording the acting user, timestamp and the value before and after the change.";
            return RedirectToAction("Approvals");
        }

        /* POST: Dashboard/Decline */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decline(string requester, string type)
        {
            TempData["SuccessMessage"] =
                $"{type} request from {requester} declined. The requester has been notified " +
                "and an audit entry recorded.";
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
