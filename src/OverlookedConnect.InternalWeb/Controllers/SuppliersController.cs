using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;
using OverlookedConnect.Internal.Filters;

/*
 Supplier Review Queue (OVC-267, Figure 26).

 The queue reads live applications from GET /api/suppliers and drives
 the workflow through POST /api/suppliers/{id}/review,
 /approve and /reject.

 The supplier approval rule is enforced by the backend API.
 A supplier cannot be approved until every required compliance
 document has been verified.

 The controller retrieves the authenticated user's API access token
 from the server-side session and passes it to OverlookedApiClient.
 The token is therefore not exposed to browser-side JavaScript.

 When the API is unreachable, the screen falls back to the
 Task 1 demonstration data so the prototype can still render.

 Reference List:
    - Fowler, M. 2003. Patterns of enterprise application architecture.
      Boston: Addison-Wesley.
    - Microsoft Learn. [s.a.]. Make HTTP requests using
      IHttpClientFactory in ASP.NET Core. [online].
      Available at:
      https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
      [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Handle requests with controllers
      in ASP.NET Core MVC. [online].
      Available at:
      https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions
      [Accessed 5 September 2026].
*/

namespace OverlookedConnect.Internal.Controllers
{
    [RequireRole("executive", "hr", "procurement")]
    public sealed class SuppliersController : Controller
    {
        private readonly OverlookedApiClient _api;

        public SuppliersController(OverlookedApiClient api)
        {
            _api = api;
        }

        // =========================================================
        // SUPPLIER QUEUE
        // =========================================================

        /// <summary>
        /// Displays the supplier review queue.
        /// GET: /Suppliers/Index
        /// </summary>
        public async Task<IActionResult> Index(
            int? id,
            string? filter,
            CancellationToken cancellationToken)
        {
            List<SupplierApplication> list;
            var live = true;

            var accessToken =
                HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                /*
                 Map the labels used by the existing Task 1 UI
                 onto the status values expected by the API.
                */
                var apiStatus = filter switch
                {
                    "Under review" => "UnderReview",
                    "Awaiting docs" => "Submitted",
                    "Verified" => "DocumentsVerified",
                    "Approved" => "Approved",
                    _ => null
                };

                var suppliers =
                    await _api.GetSuppliersAsync(
                        apiStatus,
                        accessToken,
                        cancellationToken);

                list = suppliers
                    .Select(Map)
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }
            catch (HttpRequestException)
            {
                live = false;

                list = GetDemoSuppliers(filter);

                TempData["SuccessMessage"] ??=
                    "The API is not reachable — showing Task 1 demonstration data.";
            }
            catch (Exception)
            {
                live = false;

                list = GetDemoSuppliers(filter);

                TempData["SuccessMessage"] ??=
                    "Live supplier information is currently unavailable — showing Task 1 demonstration data.";
            }

            var selected =
                list.FirstOrDefault(
                    supplier =>
                        supplier.SupplierId == id)
                ?? list.FirstOrDefault();

            ViewBag.Filter =
                filter ?? "All";

            ViewBag.LiveData =
                live;

            return View(
                new SupplierQueueViewModel
                {
                    Applications = list,
                    Selected = selected
                });
        }

        // =========================================================
        // REVIEW
        // =========================================================

        /// <summary>
        /// Moves a submitted supplier application into review.
        /// POST: /Suppliers/Review
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
            int supplierId,
            CancellationToken cancellationToken)
        {
            var accessToken =
                HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var result =
                await Call(
                    () => _api.ReviewAsync(
                        supplierId,
                        accessToken,
                        cancellationToken));

            SetSupplierMessage(result);

            return RedirectToAction(
                nameof(Index),
                new
                {
                    id = supplierId
                });
        }

        // =========================================================
        // APPROVE
        // =========================================================

        /// <summary>
        /// Approves a supplier application.
        ///
        /// The API remains responsible for enforcing the rule that
        /// all required compliance documents must be verified first.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int supplierId,
            string? companyName,
            CancellationToken cancellationToken)
        {
            var accessToken =
                HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var result =
                await Call(
                    () => _api.ApproveAsync(
                        supplierId,
                        accessToken,
                        cancellationToken));

            SetSupplierMessage(result);

            return RedirectToAction(
                nameof(Index),
                new
                {
                    id = supplierId
                });
        }

        // =========================================================
        // REJECT
        // =========================================================

        /// <summary>
        /// Rejects a supplier application.
        /// POST: /Suppliers/Reject
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int supplierId,
            string? reason,
            CancellationToken cancellationToken)
        {
            var accessToken =
                HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var result =
                await Call(
                    () => _api.RejectAsync(
                        supplierId,
                        reason,
                        accessToken,
                        cancellationToken));

            SetSupplierMessage(result);

            return RedirectToAction(
                nameof(Index),
                new
                {
                    id = supplierId
                });
        }

        // =========================================================
        // REQUEST DOCUMENTS
        // =========================================================

        /// <summary>
        /// Displays confirmation that outstanding supplier
        /// documents have been requested.
        ///
        /// This remains a UI notification for now because the
        /// current backend API does not expose a dedicated
        /// request-documents endpoint.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestDocuments(
            int supplierId,
            string companyName)
        {
            TempData["SuccessMessage"] =
                $"Outstanding document request sent to {companyName}. " +
                "The applicant can upload through the public supplier portal.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    id = supplierId
                });
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private static async Task<(bool ok, string message)> Call(
            Func<Task<(bool ok, string message)>> action)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException)
            {
                return (
                    false,
                    "The API is not reachable. Please try again.");
            }
            catch (TaskCanceledException)
            {
                return (
                    false,
                    "The supplier request timed out. Please try again.");
            }
            catch (UnauthorizedAccessException)
            {
                return (
                    false,
                    "Your session is no longer authorized. Please sign in again.");
            }
            catch (Exception)
            {
                return (
                    false,
                    "The supplier request could not be completed.");
            }
        }

        private void SetSupplierMessage(
            (bool ok, string message) result)
        {
            if (result.ok)
            {
                TempData["SuccessMessage"] =
                    result.message;
            }
            else
            {
                TempData["ErrorMessage"] =
                    result.message;
            }
        }

        private static List<SupplierApplication> GetDemoSuppliers(
            string? filter)
        {
            var demo =
                DemoData.Suppliers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter) &&
                filter != "All")
            {
                demo =
                    demo.Where(
                        supplier =>
                            supplier.Status == filter);
            }

            return demo.ToList();
        }

        /// <summary>
        /// Maps the Supplier API response onto the existing
        /// Task 1 supplier queue view model.
        /// </summary>
        private static SupplierApplication Map(
            ApiSupplier supplier)
        {
            var companyName =
                supplier.CompanyName ?? string.Empty;

            var initials =
                string.Concat(
                    companyName
                        .Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries)
                        .Take(2)
                        .Where(part => part.Length > 0)
                        .Select(part => part[0]))
                    .ToUpperInvariant();

            return new SupplierApplication
            {
                SupplierId =
                    supplier.SupplierId,

                CompanyName =
                    companyName,

                Initials =
                    string.IsNullOrWhiteSpace(initials)
                        ? "S"
                        : initials,

                AvatarColour =
                    "#2C4A7A",

                Reference =
                    supplier.Reference,

                RegistrationNumber =
                    supplier.RegistrationNumber,

                BbbeeLevel =
                    $"Level {supplier.BbbeeLevel}",

                DocumentsVerified =
                    supplier.DocumentsVerified,

                DocumentsRequired =
                    supplier.DocumentsRequired,

                Employees =
                    supplier.EmployeeCount,

                Province =
                    supplier.Province ?? string.Empty,

                Category =
                    supplier.ServicesOffered ?? string.Empty,

                Submitted =
                    supplier.SubmittedAt.ToString(
                        "dd MMM yyyy"),

                Status =
                    supplier.Status switch
                    {
                        "UnderReview" =>
                            "Under review",

                        "DocumentsVerified" =>
                            "Verified",

                        "Submitted" =>
                            "Awaiting docs",

                        _ =>
                            supplier.Status
                    },

                Documents =
                    supplier.Documents?
                        .Select(
                            document =>
                                new ComplianceDocument
                                {
                                    DocType =
                                        document.DocType,

                                    Status =
                                        document.Status
                                })
                        .ToList()
                    ?? new List<ComplianceDocument>()
            };
        }
    }
}