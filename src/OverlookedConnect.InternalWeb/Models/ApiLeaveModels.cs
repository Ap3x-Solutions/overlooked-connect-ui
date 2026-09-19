using System.Text.Json.Serialization;

namespace OverlookedConnect.Internal.Models;

/// <summary>
/// Leave request returned by the Overlooked Connect API.
/// Mirrors LeaveRequestDto from the backend API.
/// </summary>
public sealed class ApiLeaveRequest
{
    [JsonPropertyName("leaveRequestId")]
    public int LeaveRequestId { get; set; }

    [JsonPropertyName("employeeId")]
    public int EmployeeId { get; set; }

    [JsonPropertyName("employeeNumber")]
    public string EmployeeNumber { get; set; } = "";

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = "";

    [JsonPropertyName("leaveType")]
    public string LeaveType { get; set; } = "";

    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("days")]
    public decimal Days { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("approvedByUserId")]
    public int? ApprovedByUserId { get; set; }

    [JsonPropertyName("submittedAt")]
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Request body sent when HR or Executive declines leave.
/// </summary>
public sealed class ApiLeaveDecisionRequest
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
public sealed class ApiCreateLeaveRequest
{
    [JsonPropertyName("leaveType")]
    public string LeaveType { get; set; } = "";

    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}