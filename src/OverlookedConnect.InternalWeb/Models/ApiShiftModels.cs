using System.Text.Json.Serialization;

namespace OverlookedConnect.Internal.Models;

/// <summary>
/// Represents a shift returned by the Overlooked Connect Shift API.
/// </summary>
public sealed class ApiShift
{
    [JsonPropertyName("shiftId")]
    public int ShiftId { get; set; }

    [JsonPropertyName("employeeId")]
    public int EmployeeId { get; set; }

    [JsonPropertyName("employeeNumber")]
    public string EmployeeNumber { get; set; } = "";

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = "";

    [JsonPropertyName("shiftDate")]
    public DateTime ShiftDate { get; set; }

    [JsonPropertyName("shiftType")]
    public string ShiftType { get; set; } = "";

    [JsonPropertyName("site")]
    public string Site { get; set; } = "";
}