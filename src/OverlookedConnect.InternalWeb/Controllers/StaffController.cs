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
        public async Task<IActionResult> Leave(
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
                    "Only employees can access employee leave.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var result =
                await _apiClient.GetMyLeaveRequestsAsync(
                    accessToken,
                    cancellationToken);

            switch (result.Status)
            {
                case ApiLeaveStatus.Success:
                    ViewBag.LeaveRequests =
                        result.Data ??
                        Array.Empty<ApiLeaveRequest>();
                    break;

                case ApiLeaveStatus.Unauthorized:
                    HttpContext.Session.Clear();

                    TempData["Error"] =
                        "Your session has expired. Please sign in again.";

                    return RedirectToAction(
                        "Login",
                        "Account");

                case ApiLeaveStatus.Forbidden:
                    TempData["Error"] =
                        "You do not have permission to access employee leave.";

                    return RedirectToAction("Index");

                case ApiLeaveStatus.ApiUnavailable:
                    TempData["Error"] =
                        "The Overlooked Connect service is currently unavailable. Please try again.";
                    break;

                case ApiLeaveStatus.ApiFailure:
                    TempData["Error"] =
                        result.ErrorMessage ??
                        "Unable to retrieve your leave requests.";
                    break;
            }

            return View(
                new StaffLeaveRequestModel
                {
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(
            StaffLeaveRequestModel model,
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
                    "Only employees can submit leave requests.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!model.StartDate.HasValue ||
                !model.EndDate.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A start date and end date are required.");

                return View(model);
            }

            if (model.EndDate.Value.Date <
                model.StartDate.Value.Date)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The end date cannot be before the start date.");

                return View(model);
            }

            var request =
                new ApiCreateLeaveRequest
                {
                    LeaveType =
                        model.LeaveType.Trim(),

                    StartDate =
                        model.StartDate.Value.Date,

                    EndDate =
                        model.EndDate.Value.Date,

                    Reason =
                        string.IsNullOrWhiteSpace(model.Reason)
                            ? null
                            : model.Reason.Trim()
                };

            var result =
                await _apiClient.CreateLeaveRequestAsync(
                    request,
                    accessToken,
                    cancellationToken);

            switch (result.Status)
            {
                case ApiLeaveStatus.Success:
                {
                    var createdRequest =
                        result.Data;

                    TempData["SuccessMessage"] =
                        createdRequest is null
                            ? "Your leave request was submitted successfully."
                            : $"Leave request #{createdRequest.LeaveRequestId} was submitted successfully.";

                    return RedirectToAction("Index");
                }

                case ApiLeaveStatus.Unauthorized:
                    HttpContext.Session.Clear();

                    TempData["Error"] =
                        "Your session has expired. Please sign in again.";

                    return RedirectToAction(
                        "Login",
                        "Account");

                case ApiLeaveStatus.Forbidden:
                    ModelState.AddModelError(
                        string.Empty,
                        "You do not have permission to submit leave requests.");
                    break;

                case ApiLeaveStatus.ApiUnavailable:
                    ModelState.AddModelError(
                        string.Empty,
                        "The Overlooked Connect service is currently unavailable. Please try again.");
                    break;

                case ApiLeaveStatus.ApiFailure:
                    ModelState.AddModelError(
                        string.Empty,
                        result.ErrorMessage ??
                        "The leave request could not be submitted.");
                    break;
            }

            return View(model);
        }

        /* =========================================================
           INCIDENT REPORTING
           ========================================================= */

        [HttpGet]
        public IActionResult Incident()
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
                    "Only employees can submit incident reports.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            return View(
                new IncidentCaptureModel
                {
                    Site = "Forzando South"
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Incident(
            IncidentCaptureModel model,
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
                    "Only employees can submit incident reports.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var severity =
                string.Equals(
                    model.Severity,
                    "Fatal / LTI",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Fatal"
                    : model.Severity.Trim();

            var request =
                new ApiCreateIncidentRequest
                {
                    Severity =
                        severity,

                    IncidentType =
                        model.IncidentType.Trim(),

                    Description =
                        model.Description.Trim(),

                    Site =
                        model.Site.Trim(),

                    Location =
                        string.IsNullOrWhiteSpace(model.Location)
                            ? null
                            : model.Location.Trim()
                };

            var result =
                await _apiClient.CreateIncidentAsync(
                    accessToken,
                    request,
                    cancellationToken);

            switch (result.Status)
            {
                case ApiIncidentStatus.Success:
                {
                    var incident =
                        result.Data!;

                    TempData["SuccessMessage"] =
                        $"Incident {incident.Reference} was reported successfully.";

                    if (incident.RequiresEscalation)
                    {
                        TempData["IncidentEscalation"] =
                            "This incident requires escalation to the Safety team.";
                    }

                    return RedirectToAction("Index");
                }

                case ApiIncidentStatus.Unauthorized:
                    HttpContext.Session.Clear();

                    TempData["Error"] =
                        "Your session has expired. Please sign in again.";

                    return RedirectToAction(
                        "Login",
                        "Account");

                case ApiIncidentStatus.Forbidden:
                    ModelState.AddModelError(
                        string.Empty,
                        "You do not have permission to submit incident reports.");
                    break;

                case ApiIncidentStatus.ApiUnavailable:
                    ModelState.AddModelError(
                        string.Empty,
                        "The Overlooked Connect service is currently unavailable. Please try again.");
                    break;

                case ApiIncidentStatus.ApiFailure:
                    ModelState.AddModelError(
                        string.Empty,
                        result.ErrorMessage ??
                        "The incident could not be submitted.");
                    break;
            }

            return View(model);
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