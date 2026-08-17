using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;

/*
 This controller serves the Leave & Shift Scheduling module (FR-03, US-04, US-09) for the
 HR & Operations role:
  - Index:   the leave request register with approve and decline actions.
  - Roster:  the visual monthly shift roster with clash detection.
  - Approve: simulates approval, deducting the balance and writing an audit entry.

 The business rule demonstrated here is the one documented in Section 5.1.2: a leave request is
 rejected where it overlaps an existing ShiftRoster entry, or where it exceeds the employee's
 remaining balance. In the sample data L. Khumalo's request is declined automatically because
 11 to 22 August overlaps rostered shifts at Forzando South on 14 and 15 August.

 The status update, the balance deduction and the audit write are committed as a single transaction
 in the live system, so an approval can never be recorded without also recording who made it.
 */

namespace OverlookedConnect.Internal.Controllers
{
    public class LeaveController : Controller
    {
        /* GET: Leave/Index */
        public IActionResult Index(string? filter)
        {
            var requests = DemoData.LeaveRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter) && filter != "All")
            {
                requests = requests.Where(r => r.ApprovalStatus == filter);
            }

            ViewBag.Filter = filter ?? "All";
            return View(requests.ToList());
        }

        /* GET: Leave/Roster */
        public IActionResult Roster(string? site)
        {
            ViewBag.Site = site ?? "Forzando South";
            return View(DemoData.Roster);
        }

        /* POST: Leave/Approve */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int leaveId, string employeeName, decimal days)
        {
            /* For demonstration/simulation purposes, it will just show a success message in the prototype. */
            TempData["SuccessMessage"] =
                $"Leave request #{leaveId} for {employeeName} approved. {days:0.0} days deducted from the balance and an audit entry written in the same transaction.";
            return RedirectToAction("Index");
        }

        /* POST: Leave/Decline */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decline(int leaveId, string employeeName)
        {
            TempData["SuccessMessage"] =
                $"Leave request #{leaveId} for {employeeName} declined. No change was made to the leave balance.";
            return RedirectToAction("Index");
        }

        /* POST: Leave/PublishRoster */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PublishRoster()
        {
            TempData["SuccessMessage"] =
                "Roster published and employees notified. Unresolved leave clashes remain flagged for HR attention.";
            return RedirectToAction("Roster");
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Enumerable.Where Method (System.Linq). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.where> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Model validation in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation> [Accessed 14 August 2026].
        - Nowak, R., Larkin, K. and Anderson, R. [s.a.]. Routing to controller actions in ASP.NET Core MVC | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing> [Accessed 14 August 2026].
*/
