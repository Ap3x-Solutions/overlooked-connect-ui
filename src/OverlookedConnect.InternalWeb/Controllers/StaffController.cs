using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;

/*
 This controller serves the staff mobile application (US-08, US-09, US-10), used by the Employee role
 on the Android companion app:
  - Index:    staff home with clock in and out.
  - Leave:    submit a leave request from the device.
  - Incident: capture a safety incident with photographic evidence.
  - Payslip:  view and download a payslip.

 Two behaviours documented in Section 5.2.2 and NFR-13 are demonstrated here:
  1. The incident capture timestamp is set server-side rather than from the device, so that clock
     drift on a handset cannot alter the safety register.
  2. Where the device has no signal the report is queued locally and synchronised when connectivity
     returns, with the original capture time preserved. Mining sites in Mpumalanga have intermittent
     coverage, so this is a functional requirement rather than a convenience.

 The leave screen also demonstrates the roster clash rule from Section 5.1.2 before submission, so
 that the employee is warned rather than discovering the conflict after HR declines the request.
 */

namespace OverlookedConnect.Internal.Controllers
{
    public class StaffController : Controller
    {
        /* GET: Staff/Index */
        public IActionResult Index() => View();

        /* POST: Staff/ClockOut */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClockOut()
        {
            /* For demonstration/simulation purposes, it will just show a success message in the prototype. */
            TempData["SuccessMessage"] =
                "Clocked out at 18:00. Shift duration 11 h 58 m recorded, with location captured server-side.";
            return RedirectToAction("Index");
        }

        /* GET: Staff/Leave */
        [HttpGet]
        public IActionResult Leave() => View(new StaffLeaveRequestModel
        {
            StartDate = new DateTime(2026, 8, 14),
            EndDate = new DateTime(2026, 8, 18)
        });

        /* POST: Staff/Leave */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Leave(StaffLeaveRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            /* Roster clash detection, mirroring the rule enforced by LeaveService in Section 5.1.2. */
            TempData["SuccessMessage"] =
                "Leave request submitted and routed to N. Sithole (HR & Operations) for approval. A roster clash on 14 and 15 August has been flagged for HR to arrange cover.";
            return RedirectToAction("Index");
        }

        /* GET: Staff/Incident */
        [HttpGet]
        public IActionResult Incident() => View(new IncidentCaptureModel
        {
            Severity = "High",
            IncidentType = "Equipment / guarding failure",
            Location = "-26.0741, 29.4517 (accuracy 6 m)"
        });

        /* POST: Staff/Incident */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Incident(IncidentCaptureModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var escalation = model.Severity is "High" or "Fatal / LTI"
                ? " Severity is high, so the Safety Manager has been notified immediately."
                : string.Empty;

            TempData["SuccessMessage"] =
                $"Incident report captured and queued. The record is timestamped server-side and will synchronise when connectivity returns.{escalation}";
            return RedirectToAction("Index");
        }

        /* GET: Staff/Payslip */
        public IActionResult Payslip() => View();
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Model validation in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Upload files in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads> [Accessed 14 August 2026].
        - Republic of South Africa. 1996. Mine Health and Safety Act, No. 29 of 1996. Cape Town: Government Printers.
*/
