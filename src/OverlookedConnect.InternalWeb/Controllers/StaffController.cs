using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

namespace OverlookedConnect.Internal.Controllers
{
    public class StaffController : Controller
    {
        private readonly OverlookedApiClient _apiClient;

        public StaffController(
            OverlookedApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /* =========================================================
           STAFF HOME / EMPLOYEE SCHEDULE
           ========================================================= */

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var accessToken =
                HttpContext.Session.GetString("AccessToken");

            var role =
                HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!string.Equals(
                    role,
                    "Employee",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "You do not have permission to access the employee portal.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var result =
                await _apiClient.GetMyScheduleAsync(
                    accessToken,
                    cancellationToken);

            switch (result.Status)
            {
                case ApiShiftStatus.Unauthorized:
                    HttpContext.Session.Clear();

                    TempData["Error"] =
                        "Your session has expired. Please sign in again.";

                    return RedirectToAction(
                        "Login",
                        "Account");

                case ApiShiftStatus.Forbidden:
                    TempData["Error"] =
                        "You do not have permission to view this schedule.";

                    return RedirectToAction(
                        "Login",
                        "Account");

                case ApiShiftStatus.ApiUnavailable:
                    ViewBag.ScheduleError =
                        "The Overlooked Connect service is currently unavailable. Please try again.";
                    break;

                case ApiShiftStatus.ApiFailure:
                    ViewBag.ScheduleError =
                        result.ErrorMessage ??
                        "Your shift schedule could not be loaded.";
                    break;
            }

            var shifts =
                result.Data?
                    .OrderBy(x => x.ShiftDate)
                    .ToList()
                ?? new List<ApiShift>();

            var model =
                new StaffHomeViewModel
                {
                    EmployeeName =
                        HttpContext.Session.GetString("UserName") ??
                        "Employee",

                    EmployeeNumber =
                        HttpContext.Session.GetString("EmployeeNumber") ??
                        string.Empty,

                    Shifts = shifts
                };

            return View(model);
        }

        /* =========================================================
           CLOCK OUT
           ========================================================= */

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClockOut()
        {
            TempData["SuccessMessage"] =
                "Clock-out functionality is not yet connected to the API.";

            return RedirectToAction("Index");
        }

        /* =========================================================
           EMPLOYEE LEAVE
           ========================================================= */

        [HttpGet]
        public IActionResult Leave()
        {
            return View(
                new StaffLeaveRequestModel
                {
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Leave(
            StaffLeaveRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["SuccessMessage"] =
                "Leave request functionality is not yet connected to the API.";

            return RedirectToAction("Index");
        }

        /* =========================================================
           INCIDENT REPORTING
           ========================================================= */

        [HttpGet]
        public IActionResult Incident()
        {
            return View(
                new IncidentCaptureModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Incident(
            IncidentCaptureModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["SuccessMessage"] =
                "Incident reporting is not yet connected to the API.";

            return RedirectToAction("Index");
        }

        /* =========================================================
           PAYSLIP
           ========================================================= */

        [HttpGet]
        public IActionResult Payslip()
        {
            return View();
        }
    }
}