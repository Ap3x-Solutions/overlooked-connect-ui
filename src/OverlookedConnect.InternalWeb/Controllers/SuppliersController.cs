using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;

/*
 This controller serves the Supplier & SMME Administration module (FR-05, US-07) for the
 Procurement role. It presents the review queue in a master-detail layout so that an officer can
 inspect an application and its compliance documents in one screen.

 The business rule enforced in the live system is that a supplier application cannot be approved
 until every required compliance document carries a verified flag. In the sample data
 Bethal Logistics CC (Supplier #482, OVL-SUP-2026-0482) has three of five documents supplied and
 two verified, so the Approve action is unavailable until the outstanding documents are received.

 This rule is the one the Repository and Dependency Injection pattern in Section 7.1 exists to make
 testable: because SupplierAdminService depends on ISupplierRepository rather than on a concrete
 data-access class, a unit test can substitute a fake repository and assert the rule without a database.
 */

namespace OverlookedConnect.Internal.Controllers
{
    public class SuppliersController : Controller
    {
        /* GET: Suppliers/Index */
        public IActionResult Index(int? id, string? filter)
        {
            var applications = DemoData.Suppliers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter) && filter != "All")
            {
                applications = applications.Where(s => s.Status == filter);
            }

            var list = applications.ToList();
            var selected = list.FirstOrDefault(s => s.SupplierId == id) ?? list.FirstOrDefault();

            ViewBag.Filter = filter ?? "All";

            return View(new SupplierQueueViewModel { Applications = list, Selected = selected });
        }

        /* POST: Suppliers/Approve */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int supplierId, string companyName)
        {
            var supplier = DemoData.Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);

            /* Guard: mirrors the rule enforced by SupplierAdminService in Section 7.1. */
            if (supplier != null && supplier.DocumentsVerified < supplier.DocumentsRequired)
            {
                TempData["SuccessMessage"] =
                    $"{companyName} cannot be approved yet. {supplier.DocumentsRequired - supplier.DocumentsVerified} of {supplier.DocumentsRequired} compliance documents are still outstanding.";
                return RedirectToAction("Index", new { id = supplierId });
            }

            TempData["SuccessMessage"] =
                $"{companyName} approved as an OVL supplier. A vendor number has been issued and an audit entry written recording the status change.";
            return RedirectToAction("Index", new { id = supplierId });
        }

        /* POST: Suppliers/RequestDocuments */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestDocuments(int supplierId, string companyName)
        {
            TempData["SuccessMessage"] =
                $"Outstanding document request sent to {companyName}. The applicant can upload through the public supplier portal.";
            return RedirectToAction("Index", new { id = supplierId });
        }
    }
}

/*
    Reference List:
        - Fowler, M. 2003. Patterns of enterprise application architecture. Boston: Addison-Wesley.
        - Microsoft Learn. [s.a.]. Dependency injection in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Enumerable.FirstOrDefault Method (System.Linq). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.firstordefault> [Accessed 14 August 2026].
*/
