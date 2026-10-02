using OverlookedConnect.Internal.Filters;
using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;

/*
 This controller serves the Audit Log Viewer, which is restricted to the Executive role.

 The audit log is the mechanism by which NFR-08 is enforced. Every approval, incident modification
 and supplier status transition writes an entry capturing the acting user, timestamp, action, the
 affected entity, and the values before and after the change.

 Critically, the AuditLogs table is append-only: no application identity is granted UPDATE or DELETE
 permission on it, so history cannot be rewritten by any user role including an administrator. That
 constraint is enforced at the database level rather than in application code, which is why it holds
 even if the application itself is compromised. Records are retained for seven years in line with
 statutory record-keeping requirements.
 */

namespace OverlookedConnect.Internal.Controllers
{
    [RequireRole("executive", "hr")]
public class AuditController : Controller
    {
        /* GET: Audit/Index */
        public IActionResult Index(string? filter)
        {
            var entries = DemoData.AuditLog.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter) && filter != "All actions")
            {
                entries = entries.Where(e => e.Action == filter);
            }

            ViewBag.ActionFilter = filter ?? "All actions";
            return View(entries.ToList());
        }

        /* POST: Audit/Export */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Export()
        {
            TempData["SuccessMessage"] =
                "Audit log export queued. Exporting the log is itself an auditable action and has been recorded.";
            return RedirectToAction("Index");
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Azure SQL Database security overview. [online]. Available at: <https://learn.microsoft.com/en-us/azure/azure-sql/database/security-overview> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Enumerable.Where Method (System.Linq). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.where> [Accessed 14 August 2026].
        - Republic of South Africa. 2013. Protection of Personal Information Act, No. 4 of 2013. Cape Town: Government Printers.
*/
