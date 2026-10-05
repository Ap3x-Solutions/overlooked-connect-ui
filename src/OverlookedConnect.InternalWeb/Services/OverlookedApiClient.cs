using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OverlookedConnect.Internal.Models;

/*
 * Shared API integration service for the Internal Web application.
 *
 * HttpClient is supplied through the typed client registration configured
 * in Program.cs. Requests to protected API endpoints include the JWT access
 * token in the HTTP Authorization header using the Bearer authentication
 * scheme (Microsoft, [s.a.]a).
 *
 * System.Net.Http.Json is used to deserialize JSON API responses into the
 * application's typed models (Microsoft, [s.a.]b).
 *
 * The client distinguishes authentication/authorization failures and handles
 * unsuccessful HTTP responses, timeouts, connection failures and invalid JSON
 * so that controllers can provide appropriate feedback to the user.
 *
 * References:
 * Microsoft. [s.a.]a. AuthenticationHeaderValue Class.
 * [online]. Available at:
 * <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.headers.authenticationheadervalue>
 * [Accessed 4 October 2026].
 *
 * Microsoft. [s.a.]b. HttpClientJsonExtensions Class.
 * [online]. Available at:
 * <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.json.httpclientjsonextensions>
 * [Accessed 4 October 2026].
 */

namespace OverlookedConnect.Internal.Services;

/// <summary>
/// Provides communication between the Internal Web application
/// and the Overlooked Connect backend API.
/// </summary>
public sealed class OverlookedApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OverlookedApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public OverlookedApiClient(
        HttpClient httpClient,
        ILogger<OverlookedApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

/// <summary>
/// Helper model to deserialize API paged results used by several endpoints.
/// </summary>
public sealed class ApiPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
}
/// <summary>
/// Returns aggregate statistics for the Executive dashboard.
/// GET /api/reports/dashboard-summary.
/// </summary>
public async Task<ApiDashboardResult<ApiDashboardSummary>> GetDashboardSummaryAsync(
    string accessToken,
    CancellationToken cancellationToken = default)
{
    try
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Get,
            "api/reports/dashboard-summary",
            accessToken);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return ApiDashboardResult<ApiDashboardSummary>.Unauthorized();
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return ApiDashboardResult<ApiDashboardSummary>.Forbidden();
        }

        if (!response.IsSuccessStatusCode)
        {
            return ApiDashboardResult<ApiDashboardSummary>.Failure(
                $"Dashboard summary failed with HTTP {(int)response.StatusCode}.");
        }

        var result = await response.Content
            .ReadFromJsonAsync<ApiDashboardSummary>(
                JsonOptions,
                cancellationToken);

        return result is null
            ? ApiDashboardResult<ApiDashboardSummary>.Failure(
                "The API returned an empty dashboard summary.")
            : ApiDashboardResult<ApiDashboardSummary>.Success(result);
    }
    catch (TaskCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        _logger.LogWarning(
            "Dashboard summary request to the Reports API timed out.");

        return ApiDashboardResult<ApiDashboardSummary>.Unavailable();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(
            ex,
            "Unable to connect to the Reports API.");

        return ApiDashboardResult<ApiDashboardSummary>.Unavailable();
    }
    catch (JsonException ex)
    {
        _logger.LogError(
            ex,
            "Reports API returned invalid dashboard summary JSON.");

        return ApiDashboardResult<ApiDashboardSummary>.Failure(
            "The Reports API returned an invalid dashboard summary.");
    }
}

    /// <summary>
/// Returns incidents from GET /api/incidents.
/// The API returns a paged response and this method exposes the
/// incident collection through the standard incident result wrapper.
/// </summary>
public async Task<ApiIncidentResult<IReadOnlyList<ApiIncident>>> GetIncidentsAsync(
    string? status,
    string accessToken,
    CancellationToken cancellationToken = default)
{
    try
    {
        var requestUri = string.IsNullOrWhiteSpace(status)
            ? "api/incidents"
            : $"api/incidents?status={Uri.EscapeDataString(status)}";

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Get,
            requestUri,
            accessToken);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Unauthorized();
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Forbidden();
        }

        if (!response.IsSuccessStatusCode)
        {
            string? errorMessage = null;

            try
            {
                var problem = await response.Content
                    .ReadFromJsonAsync<ApiProblemDetails>(
                        JsonOptions,
                        cancellationToken);

                errorMessage =
                    problem?.Message ??
                    problem?.Detail ??
                    problem?.Title;
            }
            catch (JsonException)
            {
                // Use the fallback message below.
            }

            return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Failure(
                errorMessage ??
                $"Incident retrieval failed with HTTP {(int)response.StatusCode}.");
        }

        var paged = await response.Content
            .ReadFromJsonAsync<ApiPagedResult<ApiIncident>>(
                JsonOptions,
                cancellationToken);

        if (paged is null)
        {
            return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Failure(
                "The API returned an empty incident response.");
        }

        return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Success(
            paged.Items);
    }
    catch (TaskCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        _logger.LogWarning(
            "Incident register request to the API timed out.");

        return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Unavailable();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(
            ex,
            "Unable to connect to the Incident API.");

        return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Unavailable();
    }
    catch (JsonException ex)
    {
        _logger.LogError(
            ex,
            "Incident API returned invalid JSON.");

        return ApiIncidentResult<IReadOnlyList<ApiIncident>>.Failure(
            "The Incident API returned an invalid response.");
    }
}

    /// <summary>
    /// Returns a single incident by id: GET /api/incidents/{id}.
    /// </summary>
    public async Task<ApiIncident?> GetIncidentAsync(
        int incidentId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"api/incidents/{incidentId}",
            accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("The incidents API rejected the current session.");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new InvalidOperationException("The signed-in user does not have permission to view incidents.");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ApiIncident>(JsonOptions, cancellationToken);
    }

    // =========================================================
    // AUTHENTICATION
    // =========================================================

    /// <summary>
    /// Authenticates a user against POST /api/auth/login.
    /// </summary>
    public async Task<ApiLoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ApiLoginRequest
            {
                Email = email.Trim(),
                Password = password
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                request,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ApiLoginResult.InvalidCredentials();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Authentication API returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return ApiLoginResult.ApiFailure();
            }

            var loginResponse =
                await response.Content.ReadFromJsonAsync<ApiLoginResponse>(
                    JsonOptions,
                    cancellationToken);

            if (loginResponse is null ||
                string.IsNullOrWhiteSpace(loginResponse.AccessToken) ||
                string.IsNullOrWhiteSpace(loginResponse.Role))
            {
                _logger.LogWarning(
                    "Authentication API returned an incomplete login response.");

                return ApiLoginResult.ApiFailure();
            }

            return ApiLoginResult.Success(loginResponse);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Authentication request to the API timed out.");

            return ApiLoginResult.ApiUnavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Overlooked Connect API.");

            return ApiLoginResult.ApiUnavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Authentication API returned an invalid JSON response.");

            return ApiLoginResult.ApiFailure();
        }
    }
// =========================================================
// SSO TOKEN VALIDATION
// =========================================================

/// <summary>
/// Validates a JWT received from the PublicWeb staff sign-in flow.
///
/// The token is not trusted simply because it was posted to this
/// application. It is sent to GET /api/auth/me and the InternalWeb
/// session is only established if the backend API accepts it.
/// </summary>
public async Task<ApiIdentity?> ValidateTokenAsync(
    string accessToken,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(accessToken))
    {
        return null;
    }

    try
    {
        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "api/auth/me",
                accessToken);

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "SSO token validation returned HTTP {StatusCode}.",
                (int)response.StatusCode);

            return null;
        }

        var identity =
            await response.Content.ReadFromJsonAsync<ApiIdentity>(
                JsonOptions,
                cancellationToken);

        if (identity is null ||
            string.IsNullOrWhiteSpace(identity.Role))
        {
            _logger.LogWarning(
                "SSO token validation returned an incomplete identity.");

            return null;
        }

        return identity;
    }
    catch (TaskCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        _logger.LogWarning(
            "SSO token validation request timed out.");

        throw new HttpRequestException(
            "The sign-in service timed out.");
    }
    catch (JsonException ex)
    {
        _logger.LogError(
            ex,
            "SSO token validation returned invalid JSON.");

        return null;
    }
}
    // =========================================================
    // LEAVE
    // =========================================================

    /// <summary>
    /// Returns the complete leave register for HR and Executive users.
    /// GET /api/leave-requests
    /// </summary>
    public async Task<ApiLeaveResult<IReadOnlyList<ApiLeaveRequest>>>
        GetLeaveRequestsAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Get,
                "api/leave-requests",
                accessToken);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            return await ReadLeaveResponseAsync<
                IReadOnlyList<ApiLeaveRequest>>(
                    response,
                    cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Leave register request to the API timed out.");

            return ApiLeaveResult<
                IReadOnlyList<ApiLeaveRequest>>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Leave API.");

            return ApiLeaveResult<
                IReadOnlyList<ApiLeaveRequest>>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Leave API returned invalid JSON.");

            return ApiLeaveResult<
                IReadOnlyList<ApiLeaveRequest>>.Failure();
        }
    }

    /// <summary>
    /// Returns the signed-in employee's leave history.
    /// GET /api/leave-requests/me
    /// </summary>
    public async Task<ApiLeaveResult<IReadOnlyList<ApiLeaveRequest>>>
        GetMyLeaveRequestsAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Get,
                "api/leave-requests/me",
                accessToken);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            return await ReadLeaveResponseAsync<
                IReadOnlyList<ApiLeaveRequest>>(
                    response,
                    cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Employee leave history request timed out.");

            return ApiLeaveResult<
                IReadOnlyList<ApiLeaveRequest>>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the employee Leave API.");

            return ApiLeaveResult<
                IReadOnlyList<ApiLeaveRequest>>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Employee Leave API returned invalid JSON.");

            return ApiLeaveResult<
                IReadOnlyList<ApiLeaveRequest>>.Failure();
        }
    }

    /// <summary>
    /// Submits a new leave request for the signed-in employee.
    /// POST /api/leave-requests
    /// </summary>
    public async Task<ApiLeaveResult<ApiLeaveRequest>>
        CreateLeaveRequestAsync(
            ApiCreateLeaveRequest leaveRequest,
            string accessToken,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Post,
                "api/leave-requests",
                accessToken);

            request.Content = JsonContent.Create(
                leaveRequest,
                options: JsonOptions);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            return await ReadLeaveResponseAsync<ApiLeaveRequest>(
                response,
                cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Leave submission request timed out.");

            return ApiLeaveResult<ApiLeaveRequest>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the leave submission API.");

            return ApiLeaveResult<ApiLeaveRequest>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Leave submission API returned invalid JSON.");

            return ApiLeaveResult<ApiLeaveRequest>.Failure();
        }
    }

    /// <summary>
    /// Approves a pending leave request.
    /// POST /api/leave-requests/{id}/approve
    /// </summary>
    public async Task<ApiLeaveResult<ApiLeaveRequest>>
        ApproveLeaveAsync(
            int leaveRequestId,
            string accessToken,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Post,
                $"api/leave-requests/{leaveRequestId}/approve",
                accessToken);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            return await ReadLeaveResponseAsync<ApiLeaveRequest>(
                response,
                cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Leave approval request timed out.");

            return ApiLeaveResult<ApiLeaveRequest>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Leave approval API.");

            return ApiLeaveResult<ApiLeaveRequest>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Leave approval API returned invalid JSON.");

            return ApiLeaveResult<ApiLeaveRequest>.Failure();
        }
    }

    /// <summary>
    /// Declines a pending leave request.
    /// POST /api/leave-requests/{id}/reject
    /// </summary>
    public async Task<ApiLeaveResult<ApiLeaveRequest>>
        DeclineLeaveAsync(
            int leaveRequestId,
            string accessToken,
            string? reason = null,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Post,
                $"api/leave-requests/{leaveRequestId}/reject",
                accessToken);

            request.Content = JsonContent.Create(
                new ApiLeaveDecisionRequest
                {
                    Reason = reason
                },
                options: JsonOptions);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            return await ReadLeaveResponseAsync<ApiLeaveRequest>(
                response,
                cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Leave rejection request timed out.");

            return ApiLeaveResult<ApiLeaveRequest>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Leave rejection API.");

            return ApiLeaveResult<ApiLeaveRequest>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Leave rejection API returned invalid JSON.");

            return ApiLeaveResult<ApiLeaveRequest>.Failure();
        }
    }

    // =========================================================
    // SHIFTS
    // =========================================================

    /// <summary>
    /// Returns the weekly shift roster for a site.
    /// GET /api/shifts?site={site}&weekStart={weekStart}
    /// </summary>
    public async Task<ApiShiftResult<IReadOnlyList<ApiShift>>>
        GetShiftRosterAsync(
            string site,
            DateTime weekStart,
            string accessToken,
            CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri =
                $"api/shifts?site={Uri.EscapeDataString(site)}" +
                $"&weekStart={weekStart:yyyy-MM-dd}";

            using var request = CreateAuthenticatedRequest(
                HttpMethod.Get,
                requestUri,
                accessToken);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            return await ReadShiftResponseAsync<
                IReadOnlyList<ApiShift>>(
                    response,
                    cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Shift roster request to the API timed out.");

            return ApiShiftResult<
                IReadOnlyList<ApiShift>>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Shift API.");

            return ApiShiftResult<
                IReadOnlyList<ApiShift>>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Shift API returned invalid JSON.");

            return ApiShiftResult<
                IReadOnlyList<ApiShift>>.Failure();
        }
    }

    /// <summary>
    /// Returns the authenticated employee's shift schedule.
    /// GET /api/shifts/me
    /// </summary>
    public async Task<ApiShiftResult<IReadOnlyList<ApiShift>>>
        GetMyScheduleAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
    {
        try
        {
            using var request =
                CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "api/shifts/me",
                    accessToken);

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            return await ReadShiftResponseAsync<
                IReadOnlyList<ApiShift>>(
                    response,
                    cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Employee schedule request to the API timed out.");

            return ApiShiftResult<
                IReadOnlyList<ApiShift>>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the employee Shift API.");

            return ApiShiftResult<
                IReadOnlyList<ApiShift>>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Employee Shift API returned invalid JSON.");

            return ApiShiftResult<
                IReadOnlyList<ApiShift>>.Failure();
        }
    }

    // =========================================================
    // INCIDENTS
    // =========================================================

    /// <summary>
    /// Creates a safety incident for the authenticated employee.
    /// POST /api/incidents
    /// </summary>
    public async Task<ApiIncidentResult<ApiIncident>> CreateIncidentAsync(
        string accessToken,
        ApiCreateIncidentRequest incident,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request =
                CreateAuthenticatedRequest(
                    HttpMethod.Post,
                    "api/incidents",
                    accessToken);

            request.Content =
                JsonContent.Create(
                    incident,
                    options: JsonOptions);

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ApiIncidentResult<ApiIncident>.Unauthorized();
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return ApiIncidentResult<ApiIncident>.Forbidden();
            }

            if (!response.IsSuccessStatusCode)
            {
                string? errorMessage = null;

                try
                {
                    var problem =
                        await response.Content
                            .ReadFromJsonAsync<ApiProblemDetails>(
                                JsonOptions,
                                cancellationToken);

                    errorMessage =
                        problem?.Message ??
                        problem?.Detail ??
                        problem?.Title;
                }
                catch (JsonException)
                {
                    // Preserve the fallback message below.
                }

                return ApiIncidentResult<ApiIncident>.Failure(
                    errorMessage ??
                    $"Incident submission failed with HTTP {(int)response.StatusCode}.");
            }

            var created =
                await response.Content
                    .ReadFromJsonAsync<ApiIncident>(
                        JsonOptions,
                        cancellationToken);

            if (created is null)
            {
                return ApiIncidentResult<ApiIncident>.Failure(
                    "The API returned an empty incident response.");
            }

            return ApiIncidentResult<ApiIncident>.Success(created);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Incident API.");

            return ApiIncidentResult<ApiIncident>.Unavailable();
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Incident submission request timed out.");

            return ApiIncidentResult<ApiIncident>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Incident API returned invalid JSON.");

            return ApiIncidentResult<ApiIncident>.Failure(
                "The Incident API returned an invalid response.");
        }
    }

    // =========================================================
    // SUPPLIERS
    // =========================================================

    /// <summary>
    /// Returns supplier applications.
    /// GET /api/suppliers
    /// </summary>
    public async Task<List<ApiSupplier>> GetSuppliersAsync(
        string? status,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var requestUri =
            string.IsNullOrWhiteSpace(status) ||
            string.Equals(
                status,
                "All",
                StringComparison.OrdinalIgnoreCase)
                ? "api/suppliers"
                : $"api/suppliers?status={Uri.EscapeDataString(status)}";

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                requestUri,
                accessToken);

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "The supplier API rejected the current session.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "The signed-in user does not have permission to view suppliers.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
                   .ReadFromJsonAsync<List<ApiSupplier>>(
                       JsonOptions,
                       cancellationToken)
               ?? new List<ApiSupplier>();
    }

    /// <summary>
    /// Returns a single supplier application.
    /// GET /api/suppliers/{id}
    /// </summary>
    public async Task<ApiSupplier?> GetSupplierAsync(
        int supplierId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                $"api/suppliers/{supplierId}",
                accessToken);

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ApiSupplier>(
                JsonOptions,
                cancellationToken);
    }

    // =========================================================
    // EMPLOYEES  (OVC-99)
    // =========================================================

    /// <summary>
    /// Returns the paged employee register from GET /api/employees.
    /// The endpoint is protected by [Authorize(Roles = "HR,Executive")] and forwards
    /// search/businessUnit/status filters to the server; no filtering is done here.
    /// </summary>
    public async Task<ApiEmployeeResult<ApiPagedResult<ApiEmployee>>> GetEmployeesAsync(
        string? search,
        string? businessUnit,
        string? status,
        int page,
        int pageSize,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"api/employees?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrWhiteSpace(search))
                query += $"&search={Uri.EscapeDataString(search)}";

            if (!string.IsNullOrWhiteSpace(businessUnit))
                query += $"&businessUnit={Uri.EscapeDataString(businessUnit)}";

            if (!string.IsNullOrWhiteSpace(status))
                query += $"&status={Uri.EscapeDataString(status)}";

            using var request = CreateAuthenticatedRequest(
                HttpMethod.Get,
                query,
                accessToken);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Unauthorized();
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Forbidden();
            }

            if (!response.IsSuccessStatusCode)
            {
                string? errorMessage = null;

                try
                {
                    var problem = await response.Content
                        .ReadFromJsonAsync<ApiProblemDetails>(
                            JsonOptions,
                            cancellationToken);

                    errorMessage =
                        problem?.Message ??
                        problem?.Detail ??
                        problem?.Title;
                }
                catch (JsonException)
                {
                    // Preserve the fallback message below.
                }

                return ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Failure(
                    errorMessage ??
                    $"Employee lookup failed with HTTP {(int)response.StatusCode}.");
            }

            var result = await response.Content
                .ReadFromJsonAsync<ApiPagedResult<ApiEmployee>>(
                    JsonOptions,
                    cancellationToken);

            return result is null
                ? ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Failure(
                    "The API returned an empty employee register.")
                : ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Success(result);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Employee register request to the API timed out.");

            return ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Employees API.");

            return ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Unavailable();
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Employees API returned invalid JSON.");

            return ApiEmployeeResult<ApiPagedResult<ApiEmployee>>.Failure(
                "The Employees API returned an invalid response.");
        }
    }

    /// <summary>
    /// Moves a supplier application into review.
    /// POST /api/suppliers/{id}/review
    /// </summary>
    public Task<(bool ok, string message)> ReviewAsync(
        int supplierId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SendSupplierDecisionAsync(
            $"api/suppliers/{supplierId}/review",
            null,
            accessToken,
            cancellationToken);
    }

    /// <summary>
    /// Approves a supplier application.
    /// POST /api/suppliers/{id}/approve
    /// </summary>
    public Task<(bool ok, string message)> ApproveAsync(
        int supplierId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SendSupplierDecisionAsync(
            $"api/suppliers/{supplierId}/approve",
            null,
            accessToken,
            cancellationToken);
    }

    /// <summary>
    /// Rejects a supplier application.
    /// POST /api/suppliers/{id}/reject
    /// </summary>
    public Task<(bool ok, string message)> RejectAsync(
        int supplierId,
        string? reason,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SendSupplierDecisionAsync(
            $"api/suppliers/{supplierId}/reject",
            new
            {
                reason
            },
            accessToken,
            cancellationToken);
    }

    private async Task<(bool ok, string message)>
        SendSupplierDecisionAsync(
            string requestUri,
            object? body,
            string accessToken,
            CancellationToken cancellationToken)
    {
        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Post,
                requestUri,
                accessToken);

        if (body is not null)
        {
            request.Content =
                JsonContent.Create(
                    body,
                    options: JsonOptions);
        }

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        ApiSupplierDecision? payload = null;

        try
        {
            payload =
                await response.Content
                    .ReadFromJsonAsync<ApiSupplierDecision>(
                        JsonOptions,
                        cancellationToken);
        }
        catch (JsonException)
        {
            // Some error responses may use ProblemDetails instead.
        }

        var message =
            payload?.Message ??
            (response.IsSuccessStatusCode
                ? "Done."
                : $"The API returned {(int)response.StatusCode}.");

        return (
            response.IsSuccessStatusCode,
            message);
    }

    // =========================================================
    // SHARED RESPONSE HELPERS
    // =========================================================

    /// <summary>
    /// Converts a Shift API response into a result the MVC application can handle.
    /// </summary>
    private async Task<ApiShiftResult<T>> ReadShiftResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return ApiShiftResult<T>.Unauthorized();
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return ApiShiftResult<T>.Forbidden();
        }

        if (!response.IsSuccessStatusCode)
        {
            string? message = null;

            try
            {
                var problem =
                    await response.Content.ReadFromJsonAsync<ApiProblemDetails>(
                        JsonOptions,
                        cancellationToken);

                message =
                    problem?.Message ??
                    problem?.Detail ??
                    problem?.Title;
            }
            catch (JsonException)
            {
                // Use the MVC application's fallback error message.
            }

            _logger.LogWarning(
                "Shift API returned HTTP {StatusCode}: {Message}",
                (int)response.StatusCode,
                message ?? "No error message returned.");

            return ApiShiftResult<T>.Failure(message);
        }

        var result =
            await response.Content.ReadFromJsonAsync<T>(
                JsonOptions,
                cancellationToken);

        return result is null
            ? ApiShiftResult<T>.Failure()
            : ApiShiftResult<T>.Success(result);
    }

    /// <summary>
    /// Reads a response returned by a Leave API endpoint and converts
    /// common HTTP outcomes into a result the MVC application can handle.
    /// </summary>
    private async Task<ApiLeaveResult<T>> ReadLeaveResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return ApiLeaveResult<T>.Unauthorized();
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return ApiLeaveResult<T>.Forbidden();
        }

        if (!response.IsSuccessStatusCode)
        {
            string? message = null;

            try
            {
                var problem =
                    await response.Content.ReadFromJsonAsync<ApiProblemDetails>(
                        JsonOptions,
                        cancellationToken);

                message =
                    problem?.Message ??
                    problem?.Detail ??
                    problem?.Title;
            }
            catch (JsonException)
            {
                // The API response was not JSON in the expected format.
            }

            _logger.LogWarning(
                "Leave API returned HTTP {StatusCode}: {Message}",
                (int)response.StatusCode,
                message ?? "No error message returned.");

            return ApiLeaveResult<T>.Failure(message);
        }

        var result =
            await response.Content.ReadFromJsonAsync<T>(
                JsonOptions,
                cancellationToken);

        return result is null
            ? ApiLeaveResult<T>.Failure()
            : ApiLeaveResult<T>.Success(result);
    }

    /// <summary>
    /// Creates an authenticated request for API integrations such as
    /// leave, shifts, incidents and suppliers.
    /// </summary>
    public HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken)
    {
        var request =
            new HttpRequestMessage(
                method,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return request;
    }
}

// =========================================================
// SUPPLIER API MODELS
// =========================================================

public record ApiComplianceDocument(
    int DocId,
    string DocType,
    string Status,
    bool VerifiedFlag,
    DateTime? ExpiresOn,
    DateTime? UploadedAt);

public record ApiSupplier(
    int SupplierId,
    string Reference,
    string CompanyName,
    string RegistrationNumber,
    byte BbbeeLevel,
    int EmployeeCount,
    string? Province,
    string? ServicesOffered,
    string Status,
    string? VendorNumber,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string ContactName,
    string ContactEmail,
    int DocumentsVerified,
    int DocumentsRequired,
    List<ApiComplianceDocument> Documents);

public record ApiSupplierDecision(
    string Message,
    ApiSupplier? Supplier);

// =========================================================
// SSO IDENTITY MODEL
// =========================================================

public sealed class ApiIdentity
{
    public string? UserId { get; set; }

    public string? Name { get; set; }

    public string? Role { get; set; }

    public string? EmployeeNumber { get; set; }
}
// =========================================================
// AUTH RESULT
// =========================================================

public enum ApiLoginStatus
{
    Success,
    InvalidCredentials,
    ApiUnavailable,
    ApiFailure
}

public sealed class ApiLoginResult
{
    private ApiLoginResult(
        ApiLoginStatus status,
        ApiLoginResponse? response = null)
    {
        Status = status;
        Response = response;
    }

    public ApiLoginStatus Status { get; }

    public ApiLoginResponse? Response { get; }

    public bool IsSuccess =>
        Status == ApiLoginStatus.Success &&
        Response is not null;

    public static ApiLoginResult Success(
        ApiLoginResponse response)
        => new(
            ApiLoginStatus.Success,
            response);

    public static ApiLoginResult InvalidCredentials()
        => new(ApiLoginStatus.InvalidCredentials);

    public static ApiLoginResult ApiUnavailable()
        => new(ApiLoginStatus.ApiUnavailable);

    public static ApiLoginResult ApiFailure()
        => new(ApiLoginStatus.ApiFailure);
}

// =========================================================
// LEAVE RESULT
// =========================================================

public enum ApiLeaveStatus
{
    Success,
    Unauthorized,
    Forbidden,
    ApiUnavailable,
    ApiFailure
}

public sealed class ApiLeaveResult<T>
{
    private ApiLeaveResult(
        ApiLeaveStatus status,
        T? data = default,
        string? errorMessage = null)
    {
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public ApiLeaveStatus Status { get; }

    public T? Data { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess =>
        Status == ApiLeaveStatus.Success &&
        Data is not null;

    public static ApiLeaveResult<T> Success(
        T data)
        => new(
            ApiLeaveStatus.Success,
            data);

    public static ApiLeaveResult<T> Unauthorized()
        => new(ApiLeaveStatus.Unauthorized);

    public static ApiLeaveResult<T> Forbidden()
        => new(ApiLeaveStatus.Forbidden);

    public static ApiLeaveResult<T> Unavailable()
        => new(ApiLeaveStatus.ApiUnavailable);

    public static ApiLeaveResult<T> Failure(
        string? message = null)
        => new(
            ApiLeaveStatus.ApiFailure,
            default,
            message);
}

// =========================================================
// SHARED PROBLEM DETAILS
// =========================================================

public sealed class ApiProblemDetails
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

// =========================================================
// SHIFT RESULT
// =========================================================

public enum ApiShiftStatus
{
    Success,
    Unauthorized,
    Forbidden,
    ApiUnavailable,
    ApiFailure
}

public sealed class ApiShiftResult<T>
{
    private ApiShiftResult(
        ApiShiftStatus status,
        T? data = default,
        string? errorMessage = null)
    {
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public ApiShiftStatus Status { get; }

    public T? Data { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess =>
        Status == ApiShiftStatus.Success &&
        Data is not null;

    public static ApiShiftResult<T> Success(T data)
        => new(
            ApiShiftStatus.Success,
            data);

    public static ApiShiftResult<T> Unauthorized()
        => new(ApiShiftStatus.Unauthorized);

    public static ApiShiftResult<T> Forbidden()
        => new(ApiShiftStatus.Forbidden);

    public static ApiShiftResult<T> Unavailable()
        => new(ApiShiftStatus.ApiUnavailable);

    public static ApiShiftResult<T> Failure(
        string? message = null)
        => new(
            ApiShiftStatus.ApiFailure,
            default,
            message);
}

// =========================================================
// INCIDENT RESULT
// =========================================================

public enum ApiIncidentStatus
{
    Success,
    Unauthorized,
    Forbidden,
    ApiUnavailable,
    ApiFailure
}

public sealed class ApiIncidentResult<T>
{
    private ApiIncidentResult(
        ApiIncidentStatus status,
        T? data = default,
        string? errorMessage = null)
    {
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public ApiIncidentStatus Status { get; }

    public T? Data { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess =>
        Status == ApiIncidentStatus.Success &&
        Data is not null;

    public static ApiIncidentResult<T> Success(T data)
        => new(
            ApiIncidentStatus.Success,
            data);

    public static ApiIncidentResult<T> Unauthorized()
        => new(ApiIncidentStatus.Unauthorized);

    public static ApiIncidentResult<T> Forbidden()
        => new(ApiIncidentStatus.Forbidden);

    public static ApiIncidentResult<T> Unavailable()
        => new(ApiIncidentStatus.ApiUnavailable);

    public static ApiIncidentResult<T> Failure(
        string? message = null)
        => new(
            ApiIncidentStatus.ApiFailure,
            default,
            message);

}
// =========================================================
// DASHBOARD RESULT
// =========================================================

/// <summary>
/// Aggregate statistics returned by
/// GET /api/reports/dashboard-summary.
/// </summary>
public sealed class ApiDashboardSummary
{
    public int OpenIncidents { get; set; }

    public int EscalatedIncidents { get; set; }

    public int PendingLeaveRequests { get; set; }

    public int ActiveVacancies { get; set; }

    public Dictionary<string, int> IncidentsBySeverity { get; set; } = new();
}

public enum ApiDashboardStatus
{
    Success,
    Unauthorized,
    Forbidden,
    ApiUnavailable,
    ApiFailure
}

public sealed class ApiDashboardResult<T>
{
    private ApiDashboardResult(
        ApiDashboardStatus status,
        T? data = default,
        string? errorMessage = null)
    {
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public ApiDashboardStatus Status { get; }

    public T? Data { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess =>
        Status == ApiDashboardStatus.Success &&
        Data is not null;

    public static ApiDashboardResult<T> Success(T data)
        => new(
            ApiDashboardStatus.Success,
            data);

    public static ApiDashboardResult<T> Unauthorized()
        => new(ApiDashboardStatus.Unauthorized);

    public static ApiDashboardResult<T> Forbidden()
        => new(ApiDashboardStatus.Forbidden);

    public static ApiDashboardResult<T> Unavailable()
        => new(ApiDashboardStatus.ApiUnavailable);

    public static ApiDashboardResult<T> Failure(
        string? message = null)
        => new(
            ApiDashboardStatus.ApiFailure,
            default,
            message);
}

/* =========================================================
   EMPLOYEES
   ========================================================= */

/// <summary>
/// Client-side projection of the API's EmployeeDto, returned by GET /api/employees.
/// Property names match the JSON keys 1:1 (System.Text.Json binds case-insensitively).
/// Role, Site and LastLoginAtUtc are nullable because the API can return null for them.
/// </summary>
public sealed class ApiEmployee
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Role { get; set; }
    public string BusinessUnit { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string? Site { get; set; }
    public decimal LeaveBalance { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public enum ApiEmployeeStatus
{
    Success,
    Unauthorized,
    Forbidden,
    ApiUnavailable,
    ApiFailure
}

public sealed class ApiEmployeeResult<T>
{
    private ApiEmployeeResult(
        ApiEmployeeStatus status,
        T? data = default,
        string? errorMessage = null)
    {
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public ApiEmployeeStatus Status { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess =>
        Status == ApiEmployeeStatus.Success &&
        Data is not null;

    public static ApiEmployeeResult<T> Success(T data)
        => new(ApiEmployeeStatus.Success, data);

    public static ApiEmployeeResult<T> Unauthorized()
        => new(ApiEmployeeStatus.Unauthorized);

    public static ApiEmployeeResult<T> Forbidden()
        => new(ApiEmployeeStatus.Forbidden);

    public static ApiEmployeeResult<T> Unavailable()
        => new(ApiEmployeeStatus.ApiUnavailable);

    public static ApiEmployeeResult<T> Failure(string? message = null)
        => new(ApiEmployeeStatus.ApiFailure, default, message);
}
