using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

namespace OverlookedConnect.Internal.Controllers;

public class LeaveController : Controller
{
    private readonly OverlookedApiClient _apiClient;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(
        OverlookedApiClient apiClient,
        ILogger<LeaveController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        CancellationToken cancellationToken)
    {
        var accessToken = HttpContext.Session.GetString("AccessToken");
        var role = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToAction(
                "Login",
                "Account",
                new { returnUrl = Url.Action("Index", "Leave") });
        }

        if (!IsLeaveManager(role))
        {
            return RedirectToAction("Index", "Staff");
        }

        var result = await _apiClient.GetLeaveRequestsAsync(
            accessToken,
            cancellationToken);

        switch (result.Status)
        {
            case ApiLeaveStatus.Unauthorized:
                HttpContext.Session.Clear();

                TempData["Error"] =
                    "Your session has expired. Please sign in again.";

                return RedirectToAction(
                    "Login",
                    "Account");

            case ApiLeaveStatus.Forbidden:
                TempData["Error"] =
                    "You do not have permission to access leave management.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");

            case ApiLeaveStatus.ApiUnavailable:
                TempData["Error"] =
                    "The Overlooked Connect service is currently unavailable. Please try again.";

                return View(new List<LeaveRequest>());

            case ApiLeaveStatus.ApiFailure:
                TempData["Error"] =
                    result.ErrorMessage ??
                    "Leave requests could not be loaded.";

                return View(new List<LeaveRequest>());
        }

        var requests = result.Data ?? Array.Empty<ApiLeaveRequest>();

        var leaveRequests = requests
            .Select(MapToViewModel)
            .ToList();

        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals(
                "All",
                StringComparison.OrdinalIgnoreCase))
        {
            leaveRequests = leaveRequests
                .Where(x =>
                    x.ApprovalStatus.Equals(
                        status,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(leaveRequests);
    }
[HttpGet]
public async Task<IActionResult> Roster(
    string? site,
    DateTime? weekStart,
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
            "Account",
            new
            {
                returnUrl = Url.Action(
                    "Roster",
                    "Leave")
            });
    }

    if (!IsLeaveManager(role))
    {
        TempData["Error"] =
            "You do not have permission to access shift management.";

        return RedirectToAction(
            "Index",
            "Staff");
    }

    var selectedSite =
        string.IsNullOrWhiteSpace(site)
            ? "Forzando South"
            : site.Trim();

    var selectedWeek =
        GetMonday(
            weekStart?.Date ??
            DateTime.Today);

    var result =
        await _apiClient.GetShiftRosterAsync(
            selectedSite,
            selectedWeek,
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
                "You do not have permission to access shift management.";

            return RedirectToAction(
                "Index",
                "Dashboard");

        case ApiShiftStatus.ApiUnavailable:
            TempData["Error"] =
                "The Overlooked Connect service is currently unavailable. Please try again.";

            SetRosterViewData(
                selectedSite,
                selectedWeek);

            return View(
                new List<ShiftEntry>());

        case ApiShiftStatus.ApiFailure:
            TempData["Error"] =
                result.ErrorMessage ??
                "The shift roster could not be loaded.";

            SetRosterViewData(
                selectedSite,
                selectedWeek);

            return View(
                new List<ShiftEntry>());
    }

    var shifts =
        result.Data ??
        Array.Empty<ApiShift>();

    var roster =
        MapShiftRoster(
            shifts,
            selectedWeek);

    SetRosterViewData(
        selectedSite,
        selectedWeek);

    return View(roster);
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int id,
        CancellationToken cancellationToken)
    {
        var accessToken = HttpContext.Session.GetString("AccessToken");
        var role = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        if (!IsLeaveManager(role))
        {
            TempData["Error"] =
                "You do not have permission to approve leave requests.";

            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        var result = await _apiClient.ApproveLeaveAsync(
            id,
            accessToken,
            cancellationToken);

        switch (result.Status)
        {
            case ApiLeaveStatus.Success:
                TempData["Success"] =
                    "Leave request approved successfully.";
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
                    "You do not have permission to approve leave requests.";
                break;

            case ApiLeaveStatus.ApiUnavailable:
                TempData["Error"] =
                    "The Overlooked Connect service is currently unavailable. Please try again.";
                break;

            default:
                TempData["Error"] =
                    result.ErrorMessage ??
                    "The leave request could not be approved.";
                break;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(
        int id,
        string? reason,
        CancellationToken cancellationToken)
    {
        var accessToken = HttpContext.Session.GetString("AccessToken");
        var role = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        if (!IsLeaveManager(role))
        {
            TempData["Error"] =
                "You do not have permission to decline leave requests.";

            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        var result = await _apiClient.DeclineLeaveAsync(
            id,
            accessToken,
            reason,
            cancellationToken);

        switch (result.Status)
        {
            case ApiLeaveStatus.Success:
                TempData["Success"] =
                    "Leave request declined successfully.";
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
                    "You do not have permission to decline leave requests.";
                break;

            case ApiLeaveStatus.ApiUnavailable:
                TempData["Error"] =
                    "The Overlooked Connect service is currently unavailable. Please try again.";
                break;

            default:
                TempData["Error"] =
                    result.ErrorMessage ??
                    "The leave request could not be declined.";
                break;
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool IsLeaveManager(string? role)
    {
        return string.Equals(
                   role,
                   "HR",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   role,
                   "Executive",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static LeaveRequest MapToViewModel(
        ApiLeaveRequest request)
    {
        var initials = GetInitials(request.EmployeeName);

        return new LeaveRequest
        {
           LeaveId = request.LeaveRequestId,
            EmployeeName = string.IsNullOrWhiteSpace(
                request.EmployeeName)
                ? request.EmployeeNumber
                : request.EmployeeName,

            Initials = initials,

            AvatarColour = GetAvatarColour(
                request.EmployeeId),

            LeaveType = request.LeaveType,

            Dates =
                $"{request.StartDate:dd MMM yyyy} – {request.EndDate:dd MMM yyyy}",

            Days = request.Days,

            // The current Leave API does not expose a
            // balance-after value. Do not fabricate one.
           BalanceAfter = 0m,

            ApprovalStatus = request.Status,

            // Roster clashes are checked by the backend during
            // approval. They are not returned by the leave list API.
            RosterClash = false,
            ClashDetail = null
        };
    }
private static List<ShiftEntry> MapShiftRoster(
    IEnumerable<ApiShift> shifts,
    DateTime weekStart)
{
    var weekEnd =
        weekStart.AddDays(6);

    return shifts
        .Where(x =>
            x.ShiftDate.Date >= weekStart &&
            x.ShiftDate.Date <= weekEnd)
        .GroupBy(x => new
        {
            x.EmployeeId,
            x.EmployeeNumber,
            x.EmployeeName
        })
        .OrderBy(x => x.Key.EmployeeName)
        .Select(group =>
        {
            var pattern =
                Enumerable.Repeat(
                        "Rest",
                        7)
                    .ToArray();

            foreach (var shift in group)
            {
                var dayIndex =
                    (shift.ShiftDate.Date - weekStart).Days;

                if (dayIndex < 0 ||
                    dayIndex > 6)
                {
                    continue;
                }

                pattern[dayIndex] =
                    NormalizeShiftType(
                        shift.ShiftType);
            }

            var employeeName =
                string.IsNullOrWhiteSpace(
                    group.Key.EmployeeName)
                    ? group.Key.EmployeeNumber
                    : group.Key.EmployeeName;

            return new ShiftEntry
            {
                EmployeeName = employeeName,
                Initials =
                    GetInitials(employeeName),
                AvatarColour =
                    GetAvatarColour(
                        group.Key.EmployeeId),
                Pattern = pattern
            };
        })
        .ToList();
}

private static string NormalizeShiftType(
    string? shiftType)
{
    if (string.Equals(
        shiftType,
        "Day",
        StringComparison.OrdinalIgnoreCase))
    {
        return "Day";
    }

    if (string.Equals(
        shiftType,
        "Night",
        StringComparison.OrdinalIgnoreCase))
    {
        return "Night";
    }

    if (string.Equals(
        shiftType,
        "Leave",
        StringComparison.OrdinalIgnoreCase))
    {
        return "Leave";
    }

    return "Rest";
}

private static DateTime GetMonday(
    DateTime date)
{
    var difference =
        (7 +
         (date.DayOfWeek - DayOfWeek.Monday)) %
        7;

    return date.AddDays(-difference).Date;
}

private void SetRosterViewData(
    string site,
    DateTime weekStart)
{
    ViewBag.Site = site;
    ViewBag.WeekStart = weekStart;
    ViewBag.WeekEnd =
        weekStart.AddDays(6);
}
    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "?";
        }

        var parts = fullName
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            return parts[0][0]
                .ToString()
                .ToUpperInvariant();
        }

        return string.Concat(
            parts[0][0],
            parts[^1][0])
            .ToUpperInvariant();
    }

    private static string GetAvatarColour(int employeeId)
    {
        var colours = new[]
        {
            "#1f6f5c",
            "#365f91",
            "#7a5c3e",
            "#6b5b95",
            "#3f7d6d",
            "#805d72"
        };

        var index =
            Math.Abs(employeeId) %
            colours.Length;

        return colours[index];
    }
}