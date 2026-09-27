using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Filters;

/*
 This controller serves the Community & CSR Tracking module (FR-06, US-16) for the Executive role.
 It records CSR project spend, beneficiaries and progress, and publishes approved figures to the
 Sustainability & ESG hub on the public website.

 The link to the public platform is the point of this module: it is the same shared database serving
 both platforms, which is the "one codebase, one design system, one database" principle set out in
 Section 1.3 of the Task 1 documentation. Figures are only visible publicly once approved here.
 */

namespace OverlookedConnect.Internal.Controllers
{
    [RequireRole("executive", "hr")]
    public sealed class CommunityController : Controller
    {
        /* GET: Community/Index */
        public IActionResult Index() => View(DemoData.CsrProjects);

        /* POST: Community/Publish */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Publish()
        {
            /* For demonstration/simulation purposes, it will just show a success message in the prototype. */
            TempData["SuccessMessage"] =
                "Approved CSR figures published to the public Sustainability & ESG hub. Only approved records are exposed publicly.";
            return RedirectToAction("Index");
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Handle requests with controllers in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Views in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview> [Accessed 14 August 2026].
*/
