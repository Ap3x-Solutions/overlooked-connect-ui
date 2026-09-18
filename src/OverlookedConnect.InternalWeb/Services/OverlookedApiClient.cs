using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    /// Creates an authenticated request for future API integration
    /// such as leave, shifts, incidents and suppliers.
    /// </summary>
    public HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, requestUri);

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
        => new(ApiLoginStatus.Success, response);

    public static ApiLoginResult InvalidCredentials()
        => new(ApiLoginStatus.InvalidCredentials);

    public static ApiLoginResult ApiUnavailable()
        => new(ApiLoginStatus.ApiUnavailable);

    public static ApiLoginResult ApiFailure()
        => new(ApiLoginStatus.ApiFailure);
}