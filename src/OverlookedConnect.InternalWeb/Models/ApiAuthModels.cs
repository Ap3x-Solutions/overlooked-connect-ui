using System.Text.Json.Serialization;

namespace OverlookedConnect.Internal.Models;

/// <summary>
/// Request sent by the Internal Web application to the
/// Overlooked Connect authentication API.
/// </summary>
public sealed class ApiLoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

/// <summary>
/// Authentication response returned by POST /api/auth/login.
/// This mirrors the LoginResponse DTO exposed by the API.
/// </summary>
public sealed class ApiLoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("employeeNumber")]
    public string? EmployeeNumber { get; set; }

    [JsonPropertyName("employeeId")]
    public int? EmployeeId { get; set; }
}