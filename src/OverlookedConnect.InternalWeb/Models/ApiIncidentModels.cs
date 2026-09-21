using System.Text.Json.Serialization;

namespace OverlookedConnect.Internal.Models;

/// <summary>
/// Request sent to POST /api/incidents.
/// </summary>
public sealed class ApiCreateIncidentRequest
{
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "";

    [JsonPropertyName("incidentType")]
    public string IncidentType { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("site")]
    public string Site { get; set; } = "";

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

/// <summary>
/// Incident returned by the API after a successful submission.
/// </summary>
public sealed class ApiIncident
{
    [JsonPropertyName("incidentId")]
    public int IncidentId { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";

    [JsonPropertyName("employeeId")]
    public int EmployeeId { get; set; }

    [JsonPropertyName("employeeNumber")]
    public string EmployeeNumber { get; set; } = "";

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = "";

    [JsonPropertyName("dateReported")]
    public DateTime DateReported { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "";

    [JsonPropertyName("incidentType")]
    public string IncidentType { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("site")]
    public string Site { get; set; } = "";

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("requiresEscalation")]
    public bool RequiresEscalation { get; set; }
}