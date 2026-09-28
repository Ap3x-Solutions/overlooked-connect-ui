using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OverlookedConnect.Internal.Models;

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

    /* =========================================================
   DASHBOARD & REPORTS  (OVC-105)
   ========================================================= */

    /// <summary>
    /// GET /api/reports/dashboard-summary — aggregate counts for the Executive dashboard.
    /// The merged OVC-90 endpoint is /api/reports/dashboard-summary. Wiring reflects the actual merged route.
    /// </summary>
    public async Task<ApiDashboardResult<ApiDashboardSummary>> GetDashboardSummaryAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Get, "api/reports/dashboard-summary", accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return ApiDashboardResult<ApiDashboardSummary>.Unauthorized();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return ApiDashboardResult<ApiDashboardSummary>.Forbidden();

            if (!response.IsSuccessStatusCode)
                return ApiDashboardResult<ApiDashboardSummary>.Failure(
                    $"Dashboard summary failed with HTTP {(int)response.StatusCode}.");

            var result = await response.Content
                .ReadFromJsonAsync<ApiDashboardSummary>(JsonOptions, cancellationToken);

            return result is null
                ? ApiDashboardResult<ApiDashboardSummary>.Failure("The API returned an empty response.")
                : ApiDashboardResult<ApiDashboardSummary>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to connect to the Reports API.");
            return ApiDashboardResult<ApiDashboardSummary>.Unavailable();
        }
    }

    /// <summary>
    /// GET /api/incidents — paginated, filterable incident register. Used by the dashboard's
    /// "recent incidents" tile to pull the most recent 4 without loading the whole register.
    /// </summary>
    public async Task<ApiDashboardResult<PagedResult<ApiDashboardIncident>>> GetIncidentsAsync(
        string accessToken,
        string? search = null,
        string? status = null,
        string? severity = null,
        string? site = null,
        int page = 1,
        int pageSize = 4,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri =
                $"api/incidents?page={page}&pageSize={pageSize}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}" +
                $"&status={Uri.EscapeDataString(status ?? string.Empty)}" +
                $"&severity={Uri.EscapeDataString(severity ?? string.Empty)}" +
                $"&site={Uri.EscapeDataString(site ?? string.Empty)}";

            using var request = CreateAuthenticatedRequest(HttpMethod.Get, uri, accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return ApiDashboardResult<PagedResult<ApiDashboardIncident>>.Unauthorized();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return ApiDashboardResult<PagedResult<ApiDashboardIncident>>.Forbidden();

            if (!response.IsSuccessStatusCode)
                return ApiDashboardResult<PagedResult<ApiDashboardIncident>>.Failure(
                    $"Incident lookup failed with HTTP {(int)response.StatusCode}.");

            var result = await response.Content
                .ReadFromJsonAsync<PagedResult<ApiDashboardIncident>>(JsonOptions, cancellationToken);

            return result is null
                ? ApiDashboardResult<PagedResult<ApiDashboardIncident>>.Failure("The API returned an empty response.")
                : ApiDashboardResult<PagedResult<ApiDashboardIncident>>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to connect to the Incidents API.");
            return ApiDashboardResult<PagedResult<ApiDashboardIncident>>.Unavailable();
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

/* =========================================================
   DASHBOARD & REPORTS  (OVC-105)
   ========================================================= */

/// <summary>
/// Client-side projection of the API's DashboardSummaryDto, returned by
/// GET /api/reports/dashboard-summary. Property names match the JSON keys 1:1.
/// </summary>
public sealed class ApiDashboardSummary
{
    public int OpenIncidents { get; set; }
    public int EscalatedIncidents { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int ActiveVacancies { get; set; }
    public Dictionary<string, int> IncidentsBySeverity { get; set; } = new();
}

/// <summary>
/// Client-side projection of the API's IncidentDto. Only the fields the dashboard tile
/// renders are modelled here. Enums are serialised as strings by the API, so Severity
/// and Status are typed as string.
/// </summary>
public sealed class ApiDashboardIncident
{
    public int Id { get; set; }
    public string Reference { get; set; } = "";
    public string IncidentType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Site { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime DateReported { get; set; }

    public bool RequiresEscalation =>
        Severity is "High" or "Fatal";
}

/// <summary>
/// Mirror of the API's Application/Common/PagedResult;. Kept in the UI project so the
/// Razor view can consume a strongly-typed paged result without referencing the Application layer.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
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
        => new(ApiDashboardStatus.Success, data);

    public static ApiDashboardResult<T> Unauthorized()
        => new(ApiDashboardStatus.Unauthorized);

    public static ApiDashboardResult<T> Forbidden()
        => new(ApiDashboardStatus.Forbidden);

    public static ApiDashboardResult<T> Unavailable()
        => new(ApiDashboardStatus.ApiUnavailable);

    public static ApiDashboardResult<T> Failure(string? message = null)
        => new(ApiDashboardStatus.ApiFailure, default, message);
}