using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Filters;

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
    [RequireRole("executive", "hr", "procurement", "safety")]
    public sealed class DashboardController : Controller
    {
        /* GET: Dashboard/Index */
        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                Approvals = DemoData.Approvals,
                RecentIncidents = DemoData.Incidents.Take(4).ToList(), /* [Microsoft Learn, [s.a.]] */
                Production = DemoData.ProductionByMonth,
                Workforce = DemoData.WorkforceByUnit
            };
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
