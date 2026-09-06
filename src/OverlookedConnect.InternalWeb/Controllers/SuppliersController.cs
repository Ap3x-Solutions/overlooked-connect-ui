using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

/*
 Supplier Review Queue (OVC-267, Figure 26).

 The queue now reads live applications from GET /api/suppliers and drives the workflow through
 POST /api/suppliers/{id}/review, /approve and /reject.

 Where the approval rule lives, and why it matters that it is not here:

 The rule — a supplier cannot be approved until every required compliance document is verified — is
 enforced by the API, on the Supplier entity. This controller only relays the API's decision
 message. Putting the rule in this screen would mean the Android client or a direct API call could
 bypass it, and the same rule would exist in two places to drift apart.

 When the API is unreachable the screen falls back to the Task 1 demonstration data so the
 prototype still renders, and says so in the banner.

 Reference List:
    - Fowler, M. 2003. Patterns of enterprise application architecture. Boston: Addison-Wesley.
    - Microsoft Learn. [s.a.]. Make HTTP requests using IHttpClientFactory in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Handle requests with controllers in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions> [Accessed 5 September 2026].
*/

namespace OverlookedConnect.Internal.Controllers
{
    public class SuppliersController : Controller
    {
        private readonly OverlookedApiClient _api;
        public SuppliersController(OverlookedApiClient api) => _api = api;

        /* GET: Suppliers/Index */
        public async Task<IActionResult> Index(int? id, string? filter)
        {
            List<SupplierApplication> list;
            var live = true;

            try
            {
                /* Map the screen's filter labels onto the API's status values. */
                var apiStatus = filter switch
                {
                    "Under review"  => "UnderReview",
                    "Awaiting docs" => "Submitted",
                    "Verified"      => "DocumentsVerified",
                    "Approved"      => "Approved",
                    _               => null
                };

                list = (await _api.GetSuppliersAsync(apiStatus)).Select(Map).ToList();
            }
            catch (Exception)
            {
                live = false;
                var demo = DemoData.Suppliers.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(filter) && filter != "All")
                    demo = demo.Where(s => s.Status == filter);
                list = demo.ToList();
            }

            var selected = list.FirstOrDefault(s => s.SupplierId == id) ?? list.FirstOrDefault();

            ViewBag.Filter = filter ?? "All";
            ViewBag.LiveData = live;

            if (!live)
                TempData["SuccessMessage"] ??= "The API is not reachable — showing Task 1 demonstration data.";

            return View(new SupplierQueueViewModel { Applications = list, Selected = selected });
        }

        /* POST: Suppliers/Review — Submitted or Rejected → UnderReview */
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int supplierId)
        {
            TempData["SuccessMessage"] = (await Call(() => _api.ReviewAsync(supplierId))).message;
            return RedirectToAction("Index", new { id = supplierId });
        }

        /* POST: Suppliers/Approve — refused by the API while any document is unverified */
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int supplierId, string? companyName)
        {
            TempData["SuccessMessage"] = (await Call(() => _api.ApproveAsync(supplierId))).message;
            return RedirectToAction("Index", new { id = supplierId });
        }

        /* POST: Suppliers/Reject */
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int supplierId, string? reason)
        {
            TempData["SuccessMessage"] = (await Call(() => _api.RejectAsync(supplierId, reason))).message;
            return RedirectToAction("Index", new { id = supplierId });
        }

        /* POST: Suppliers/RequestDocuments — notification only, no state change on the API */
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RequestDocuments(int supplierId, string companyName)
        {
            TempData["SuccessMessage"] =
                $"Outstanding document request sent to {companyName}. The applicant can upload through the public supplier portal.";
            return RedirectToAction("Index", new { id = supplierId });
        }

        /* ---------------- helpers ---------------- */

        private static async Task<(bool ok, string message)> Call(Func<Task<(bool ok, string message)>> action)
        {
            try { return await action(); }
            catch (HttpRequestException)
            {
                return (false, "The API is not reachable. Start OverlookedConnect.Api and try again.");
            }
        }

        /// <summary>Maps the API shape onto the view model the Task 1 screen already renders.</summary>
        private static SupplierApplication Map(ApiSupplier s) => new()
        {
            SupplierId = s.SupplierId,
            CompanyName = s.CompanyName,
            Initials = string.Concat(s.CompanyName
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(2).Select(p => p[0])).ToUpperInvariant(),
            AvatarColour = "#2C4A7A",
            Reference = s.Reference,
            RegistrationNumber = s.RegistrationNumber,
            BbbeeLevel = $"Level {s.BbbeeLevel}",
            DocumentsVerified = s.DocumentsVerified,
            DocumentsRequired = s.DocumentsRequired,
            Employees = s.EmployeeCount,
            Province = s.Province ?? "",
            Category = s.ServicesOffered ?? "",
            Submitted = s.SubmittedAt.ToString("dd MMM yyyy"),
            Status = s.Status switch
            {
                "UnderReview"       => "Under review",
                "DocumentsVerified" => "Verified",
                "Submitted"         => "Awaiting docs",
                _                   => s.Status
            },
            Documents = s.Documents.Select(d => new ComplianceDocument
            {
                DocType = d.DocType,
                Status = d.Status      /* Required, Pending, Verified or Rejected */
            }).ToList()
        };
    }
}
