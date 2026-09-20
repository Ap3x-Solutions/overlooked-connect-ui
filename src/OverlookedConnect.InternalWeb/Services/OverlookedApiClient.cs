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
/// <summary>
/// Returns the weekly shift roster for a site.
/// GET /api/shifts?site={site}&amp;weekStart={weekStart}
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
            // The MVC application will use its fallback message.
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

public sealed class ApiProblemDetails
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

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