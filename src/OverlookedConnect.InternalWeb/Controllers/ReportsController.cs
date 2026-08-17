using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;

/*
 This controller serves the Reports & Analytics module (FR-07, US-16). It generates and exports HR,
 safety, supplier and ESG reports in PDF and spreadsheet formats.

 Report generation is simulated in Task 1. In Task 2 the export is produced server-side and the
 request is scoped to the signed-in user's role, so that a Safety Officer cannot export payroll data
 and an HR administrator cannot export the audit log. That scoping is part of the least-privilege
 principle described in Section 8.1.
 */

namespace OverlookedConnect.Internal.Controllers
{
    public class ReportsController : Controller
    {
        /* GET: Reports/Index */
        public IActionResult Index() => View(new ReportRequestModel());

        /* POST: Reports/Generate */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Generate(ReportRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            /* For demonstration/simulation purposes, it will just show a success message in the prototype. */
            TempData["SuccessMessage"] =
                $"{model.ReportType} generated for {model.DateRange} ({model.BusinessUnit}) in {model.Format} format. The export has been queued for download.";
            return RedirectToAction("Index");
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Model validation in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Tag Helpers in forms in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms> [Accessed 14 August 2026].
*/
