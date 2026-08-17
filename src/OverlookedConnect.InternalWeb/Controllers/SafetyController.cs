using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;

/*
 This controller serves the Safety & Incident Reporting module (FR-04, US-05, US-06) for the
 Safety Officer role:
  - Index:        the incident register with a master-detail layout.
  - UpdateStatus: advances an incident through its lifecycle.

 The incident lifecycle modelled here follows Section 5.2.2 of the documentation:
   Reported > Acknowledged > Under Investigation > Corrective Action Assigned > Verification > Closed
 with an Escalated state entered where severity is classified as fatal or as a lost-time injury,
 which triggers the statutory reporting obligation to the DMRE. Escalated is deliberately not a
 terminal state, because escalation does not discharge the duty to investigate.

 Verification can also send work backwards: where a corrective action proves ineffective the record
 returns to Corrective Action Assigned rather than closing, which is what prevents the register
 recording unresolved hazards as resolved.
 */

namespace OverlookedConnect.Internal.Controllers
{
    public class SafetyController : Controller
    {
        /* The ordered lifecycle stages, used by the view to render the progress tracker. */
        public static readonly string[] Lifecycle =
        {
            "Reported", "Acknowledged", "Investigating", "Corrective action", "Verification", "Closed"
        };

        /* GET: Safety/Index */
        public IActionResult Index(string? id, string? filter)
        {
            var incidents = DemoData.Incidents.AsEnumerable();

            if (filter == "Open")
            {
                incidents = incidents.Where(i => i.InvestigationStatus != "Closed");
            }
            else if (filter == "Closed")
            {
                incidents = incidents.Where(i => i.InvestigationStatus == "Closed");
            }

            var list = incidents.ToList();
            var selected = list.FirstOrDefault(i => i.IncidentId == id) ?? list.FirstOrDefault();

            ViewBag.Filter = filter ?? "All";

            return View(new SafetyViewModel { Incidents = list, Selected = selected });
        }

        /* POST: Safety/AssignAction */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignAction(string incidentId)
        {
            /* For demonstration/simulation purposes, it will just show a success message in the prototype. */
            TempData["SuccessMessage"] =
                $"Corrective action assigned for {incidentId}. The owner and due date have been recorded and the incident moved to Corrective action.";
            return RedirectToAction("Index", new { id = incidentId });
        }

        /* POST: Safety/Escalate */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Escalate(string incidentId)
        {
            TempData["SuccessMessage"] =
                $"{incidentId} escalated. A statutory report is required to the Department of Mineral Resources and Energy; the investigation remains open.";
            return RedirectToAction("Index", new { id = incidentId });
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Enumerable.FirstOrDefault Method (System.Linq). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.firstordefault> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Views in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview> [Accessed 14 August 2026].
        - Republic of South Africa. 1996. Mine Health and Safety Act, No. 29 of 1996. Cape Town: Government Printers.
*/
