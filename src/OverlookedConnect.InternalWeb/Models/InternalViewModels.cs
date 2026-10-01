using System.ComponentModel.DataAnnotations; /* [Microsoft Learn, [s.a.]] */
using Microsoft.AspNetCore.Http;

/*
 Internal View Models
 This file contains the view models used by the Overlooked Connect Internal Operations Platform.
 View models carry data between the controllers and the Razor views, and hold the validation rules
 that produce the on-screen error messages.

 As with the public portal, these are validation models only for Task 1. No data is persisted; the
 POST actions display a confirmation message so that the interaction can be demonstrated. Persistence
 through Entity Framework Core and Azure SQL Database follows in Task 2, as set out in Section 6.1
 of the Task 1 documentation.
 */

namespace OverlookedConnect.Internal.Models
{
   public class LoginModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = "";

    [Display(Name = "Keep me signed in on this device")]
    public bool RememberMe { get; set; }
}
    public class StaffLeaveRequestModel
    {
        [Required(ErrorMessage = "Leave type is required.")]
        [Display(Name = "Leave type")]
        public string LeaveType { get; set; } = "Annual";

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "From")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "To")]
        public DateTime? EndDate { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }

        [DataType(DataType.Upload)]
        [Display(Name = "Supporting document")]
        public IFormFile? SupportingDocument { get; set; }
    }

   public class IncidentCaptureModel
{
    [Required(ErrorMessage = "Severity must be selected from the list.")]
    public string Severity { get; set; } = "";

    [Required(ErrorMessage = "Incident type is required.")]
    [Display(Name = "Incident type")]
    public string IncidentType { get; set; } = "";

    [Required(ErrorMessage = "A description is required.")]
    [StringLength(
        500,
        MinimumLength = 10,
        ErrorMessage = "Description must be between 10 and 500 characters.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Site is required.")]
    [Display(Name = "Site")]
    public string Site { get; set; } = "Forzando South";

    [Display(Name = "Location")]
    public string? Location { get; set; }

    /*
     * The current incident API does not support photographic
     * evidence yet. This property is retained so that the
     * existing UI model remains compatible with the prototype.
     */
    [Display(Name = "Photographic evidence")]
    public List<IFormFile>? Photos { get; set; }
}

    public class ReportRequestModel
    {
        [Required(ErrorMessage = "Select a report type.")]
        [Display(Name = "Report type")]
        public string ReportType { get; set; } = "Safety incident summary";

        [Required(ErrorMessage = "Select a date range.")]
        [Display(Name = "Date range")]
        public string DateRange { get; set; } = "01 Jul â€“ 31 Jul 2026";

        [Display(Name = "Business unit")]
        public string BusinessUnit { get; set; } = "All units";

        [Required(ErrorMessage = "Select an export format.")]
        public string Format { get; set; } = "PDF";
    }

    /* ---------------- Page view models ---------------- */

  public class DashboardViewModel
{
    public List<ApprovalItem> Approvals { get; set; } = new();
    public List<Incident> RecentIncidents { get; set; } = new();
    public int[] Production { get; set; } = Array.Empty<int>();
    public List<(string Unit, int Headcount, string Colour)> Workforce { get; set; } = new();

    // Live dashboard statistics from GET /api/reports/dashboard-summary.
    public OverlookedConnect.Internal.Services.ApiDashboardSummary? Summary { get; set; }
}
    public class SupplierQueueViewModel
    {
        public List<SupplierApplication> Applications { get; set; } = new();
        public SupplierApplication? Selected { get; set; }
    }

    public class SafetyViewModel
    {
        public List<Incident> Incidents { get; set; } = new();
        public Incident? Selected { get; set; }
    }


public class StaffHomeViewModel
{
    public string EmployeeName { get; set; } = "";

    public string EmployeeNumber { get; set; } = "";

    public List<ApiShift> Shifts { get; set; } = new();

    public ApiShift? NextShift =>
        Shifts
            .Where(x => x.ShiftDate.Date >= DateTime.Today)
            .OrderBy(x => x.ShiftDate)
            .FirstOrDefault();

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(EmployeeName))
            {
                return "E";
            }

            var parts = EmployeeName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            return string.Concat(
                parts
                    .Take(2)
                    .Select(x => char.ToUpperInvariant(x[0])));
        }
    }
}
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Model Binding in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Model validation in ASP.NET Core MVC. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. System.ComponentModel.DataAnnotations Namespace. [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Upload files in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads> [Accessed 14 August 2026].
*/
