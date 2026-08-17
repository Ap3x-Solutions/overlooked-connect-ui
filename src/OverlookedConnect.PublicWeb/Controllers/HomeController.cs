using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.PublicWeb.Models;

/*
 This controller handles the main pages of the OverlookedConnect.PublicWeb website, including:
  - Index: The homepage.
  - Operations: Information about the company's operations.
  - Leadership: Details about the company's leadership team.
  - Sustainability: Information on sustainability initiatives.
  - Careers: Job listings and application process.
  - Suppliers: Supplier registration and information.
  - Contact: Contact form for inquiries.
  
 It also includes POST actions for handling form submissions for job applications, supplier registrations, and contact inquiries. 
 Success messages are displayed to users upon successful submission of forms. 
 Note: The actual processing of form data (e.g., saving to a database, sending emails) is not implemented in this controller and should be handled in the respective POST actions.
 AntiForgery tokens are used for security in form submissions and added for foundational secruity layer for Task 2 data integratiom. 
 */

namespace OverlookedConnect.PublicWeb.Controllers
{
    public class HomeController : Controller
    {
        /* GET: Home/Index [Microsoft Learn, [s.a.]] */
        public IActionResult Index() => View();

        /* GET: Home/Operations */
        public IActionResult Operations() => View();

        /* GET: Home/Leadership */
        public IActionResult Leadership() => View();

        /* GET: Home/Sustainability */
        public IActionResult Sustainability() => View();

        /* GET: Home/Careers */
        public IActionResult Careers() => View();

        /* GET: Home/Suppliers */
        public IActionResult Suppliers() => View();

        /* GET: Home/Contact */
        public IActionResult Contact() => View();

        /* GET: Home/CareersApply */
        [HttpGet]
        public IActionResult CareersApply(string position) /* [Smith & Addie, [s.a.]] */
        {
            var model = new JobApplicationModel
            {
                Position = position ?? "Not Specified"
            };
            return View(model);
        }

        /* POST: Home/CareersApply [Nowak, Larkin & Anderson, [s.a.]] */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CareersApply(JobApplicationModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            /*  For demonstartion/simulation purposes, it will just show success message in the prototype. */

            TempData["SuccessMessage"] = "Your job application has been successfully submitted! Our HR team will review your application and contact you if your skills match our requirements.";
            return RedirectToAction("Careers");
        }

        /* POST: Home/SubmitSupplier [Hasan & Anderson, [s.a.]] */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitSupplier(SupplierRegistrationModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Suppliers", model);
            }

            /*  For demonstartion/simulation purposes, it will just show success message in the prototype. */

            TempData["SuccessMessage"] = "Supplier registration received. Our procurement team will review your documents and contact you within 5-7 business days.";
            return RedirectToAction("Suppliers");
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

            /*  For demonstartion/simulation purposes, it will just show success message in the prototype. */

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
*/