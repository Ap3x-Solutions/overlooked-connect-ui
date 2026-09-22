using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.PublicWeb.Models;
using OverlookedConnect.PublicWeb.Services;

/*
 This controller handles the main pages of the OverlookedConnect.PublicWeb website, including:
  - Index: The homepage.
  - Operations: Information about the company's operations.
  - Leadership: Details about the company's leadership team.
  - Sustainability: Information on sustainability initiatives.
  - Careers: Job listings and application process.
  - Suppliers: Supplier registration and information.
  - Contact: Contact form for inquiries.

 Task 2 changes (OVC-267):

  - SubmitSupplier now posts to POST /api/suppliers instead of showing a simulated message. The
    applicant receives a real reference number, and the application appears immediately in the
    internal review queue — the same record, one database (Section 1.3).
  - Careers reads live vacancies from GET /api/vacancies, falling back to the static page content
    when the API is unreachable so the site never breaks for a visitor.
  - SupplierStatus renders a signed-in supplier's own application from GET /api/suppliers/mine.

 Job applications and contact enquiries remain simulated; their endpoints are assigned to other
 tickets and will be connected the same way.

 AntiForgery tokens are used for security in form submissions and added for foundational security
 layer for Task 2 data integration.
 */

namespace OverlookedConnect.PublicWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly OverlookedApiClient _api;
        public HomeController(OverlookedApiClient api) => _api = api;

        /* GET: Home/Index [Microsoft Learn, [s.a.]] */
        public IActionResult Index() => View();

        /* GET: Home/Operations */
        public IActionResult Operations() => View();

        /* GET: Home/Leadership */
        public IActionResult Leadership() => View();

        /* GET: Home/Sustainability */
        public IActionResult Sustainability() => View();

        /* GET: Home/Careers — OVC-250: live vacancies from the API */
        public async Task<IActionResult> Careers()
        {
            try
            {
                ViewBag.Vacancies = await _api.GetVacanciesAsync();
                ViewBag.LiveVacancies = true;
            }
            catch (HttpRequestException)
            {
                /* The careers page must still render for a visitor if the API is down. */
                ViewBag.Vacancies = new List<ApiVacancy>();
                ViewBag.LiveVacancies = false;
            }
            return View();
        }

        /* GET: Home/Suppliers */
        public IActionResult Suppliers() => View();

        /* GET: Home/Contact */
        public IActionResult Contact() => View();

       /* GET: Home/CareersApply */
[HttpGet]
public async Task<IActionResult> CareersApply(
    int vacancyId,
    string? position)
{
    if (vacancyId <= 0)
    {
        TempData["SuccessMessage"] =
            "Please select one of the currently available vacancies.";

        return RedirectToAction(nameof(Careers));
    }

    try
    {
        var vacancy = await _api.GetVacancyAsync(vacancyId);

        if (vacancy is null)
        {
            TempData["SuccessMessage"] =
                "The selected vacancy could not be found.";

            return RedirectToAction(nameof(Careers));
        }

        var model = new JobApplicationModel
        {
            VacancyId = vacancy.VacancyId,
            Position = vacancy.Title
        };

        return View(model);
    }
    catch (HttpRequestException)
    {
        var model = new JobApplicationModel
        {
            VacancyId = vacancyId,
            Position = position ?? "Selected vacancy"
        };

        return View(model);
    }
}

/* POST: Home/CareersApply */
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CareersApply(
    JobApplicationModel model,CancellationToken cancellationToken)
{
    if (!model.PopiaConsent)
        {
            ModelState.AddModelError(
                nameof(model.PopiaConsent),
                "You must accept the POPIA consent to proceed.");
        }
    
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    try
    {
        var (ok, result, message) =
            await _api.SubmitJobApplicationAsync(
                model.VacancyId,
                model.FullName,
                model.Email,
                model.Phone,
                model.IdNumber,
                model.Qualification,
                model.YearsExperience,
                model.PopiaConsent,
                model.ResumeFile);

        if (!ok)
        {
            ModelState.AddModelError(
                string.Empty,
                message);

            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Your application for {result!.VacancyTitle} " +
            $"has been successfully submitted. " +
            $"Application reference: {result.ApplicationId}.";

        return RedirectToAction(nameof(Careers));
    }
    catch (HttpRequestException)
    {
        ModelState.AddModelError(
            string.Empty,
            "We could not reach the recruitment service. " +
            "Please try again shortly.");

        return View(model);
    }
}

        /* POST: Home/SubmitSupplier — OVC-247 / OVC-267 [Hasan & Anderson, [s.a.]] */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSupplier(SupplierRegistrationModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Suppliers", model);
            }

            /* The API expects a numeric B-BBEE level; the form offers "Level 1 — 135% recognition". */
            byte bbbeeLevel = 1;
            var digits = new string((model.BbbeeLevel ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length > 0 && byte.TryParse(digits[..1], out var parsed)) bbbeeLevel = parsed;

            var payload = new
            {
                companyName = model.RegisteredCompanyName,
                registrationNumber = model.CompanyRegistrationNumber,
                vatNumber = model.VatNumber,
                bbbeeLevel,
                employeeCount = model.NumberOfEmployees ?? 1,
                province = model.Province,
                servicesOffered = model.GoodsServicesOffered,
                contactName = model.ContactFullName,
                contactEmail = model.ContactEmail,
                contactNumber = model.ContactNumber,
                password = model.Password,
                popiaConsent = model.PopiaConsent
            };

            try
            {
                var (ok, result, message) = await _api.RegisterSupplierAsync(payload);

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, message);
                    return View("Suppliers", model);
                }

                TempData["SuccessMessage"] =
                    $"{message} Your reference is {result!.Reference} — quote it in any correspondence.";
                return RedirectToAction("Suppliers");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty,
                    "We could not reach the registration service. Please try again shortly.");
                return View("Suppliers", model);
            }
        }

        /* GET: Home/SupplierStatus — the applicant's own application (Figure 22) */
        public async Task<IActionResult> SupplierStatus()
        {
            var token = HttpContext.Session.GetString("SupplierToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["SuccessMessage"] = "Sign in to view your application status.";
                return RedirectToAction("Login", "StaffAccess");
            }

            try
            {
                var supplier = await _api.GetMySupplierAsync(token);
                if (supplier is null)
                {
                    HttpContext.Session.Remove("SupplierToken");
                    return RedirectToAction("Login", "StaffAccess");
                }

                return View(new SupplierStatusModel
                {
                    Reference = supplier.Reference,
                    CompanyName = supplier.CompanyName,
                    Status = supplier.Status,
                    VendorNumber = supplier.VendorNumber,
                    SubmittedAt = supplier.SubmittedAt,
                    DocumentsVerified = supplier.DocumentsVerified,
                    DocumentsRequired = supplier.DocumentsRequired,
                    Documents = supplier.Documents.Select(d => (d.DocType, d.Status)).ToList()
                });
            }
            catch (HttpRequestException)
            {
                TempData["SuccessMessage"] = "The service is not reachable at the moment. Please try again shortly.";
                return RedirectToAction("Suppliers");
            }
        }

        /* POST: Home/SubmitContact */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitContact(ContactEnquiryModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Contact", model);
            }

            /*  For demonstartion/simulation purposes, it will just show success message in the prototype.
                Connects to POST /api/enquiries under a later ticket. */

            var routingDesk = model.Reason switch
            {
                "Investment" => "the Office of the CFO",
                "Partnership" => "Business Development",
                "Procurement" => "the Supply Chain team",
                "Media" => "Corporate Communications",
                _ => "the Executive Desk"
            };
            TempData["SuccessMessage"] = $"Enquiry successfully routed to {routingDesk}. A representative will respond to your query shortly.";
            return RedirectToAction("Contact");
        }
    }
}

/*
    Reference List:
        - Smith, S. and Addie, S. [s.a.]. Handle requests with controllers in ASP.NEt Core MVC | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions> [14 August 2026].
        - Microsoft Learn. [s.a.]. Model validation in ASP.NET Core MVC | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation> [14 August 2026].
        - Hasan, F. and Anderson, R. [s.a.]. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery> [14 August 2026].
        - Nowak, R., Larkin, K., and Anderson, R. [s.a.]. Routing to controller actions in ASP.NET Core MVC | Microsoft Learn. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing> [14 August 2026].
        - Microsoft Learn. [s.a.]. Make HTTP requests using IHttpClientFactory in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests> [Accessed 5 September 2026].
*/
