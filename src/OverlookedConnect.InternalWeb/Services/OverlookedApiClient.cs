using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

/*
 Typed HTTP client for the shared Overlooked Connect REST API, used by the internal operations
 platform (OVC-267).

 The bearer token is read from the server-side session and attached here, in one place, so no
 controller ever handles it directly. The session cookie is HttpOnly, so the token is never exposed
 to client-side script — which matters because a JWT in browser-accessible storage is readable by
 any injected script (Section 8.1).

 Reference List:
    - Microsoft Learn. [s.a.]. Make HTTP requests using IHttpClientFactory in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Session and state management in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state> [Accessed 5 September 2026].
    - OWASP. [s.a.]. HTML5 Security Cheat Sheet: Local Storage. [online]. Available at: <https://cheatsheetseries.owasp.org/cheatsheets/HTML5_Security_Cheat_Sheet.html> [Accessed 5 September 2026].
*/

namespace OverlookedConnect.Internal.Services
{
    public record ApiLoginResponse(
        string AccessToken, DateTime ExpiresAtUtc, int UserId, string FullName,
        string Email, string Role, string? EmployeeNumber, int? EmployeeId);

    public record ApiMessage(string Message);

    public record ApiComplianceDocument(int DocId, string DocType, string Status, bool VerifiedFlag,
        DateTime? ExpiresOn, DateTime? UploadedAt);

    public record ApiSupplier(int SupplierId, string Reference, string CompanyName, string RegistrationNumber,
        byte BbbeeLevel, int EmployeeCount, string? Province, string? ServicesOffered, string Status,
        string? VendorNumber, DateTime SubmittedAt, DateTime? ReviewedAt, string ContactName,
        string ContactEmail, int DocumentsVerified, int DocumentsRequired, List<ApiComplianceDocument> Documents);

    public record ApiDecision(string Message, ApiSupplier? Supplier);

    public record ApiIdentity(string? UserId, string? Name, string? Role, string? EmployeeNumber);

    public class OverlookedApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _ctx;
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public OverlookedApiClient(HttpClient http, IHttpContextAccessor ctx)
        {
            _http = http;
            _ctx = ctx;
        }

        /// <summary>Attaches the session's bearer token to the outgoing request.</summary>
        private void Authorise()
        {
            var token = _ctx.HttpContext?.Session.GetString("ApiToken");
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        }

        /* ---------------- authentication ---------------- */

        public async Task<ApiLoginResponse?> LoginAsync(string email, string password)
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            return res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<ApiLoginResponse>(Json)
                : null;
        }

        /// <summary>
        /// Validates a token handed over from the public website (OVC-267 single sign-on).
        /// The token is never trusted on arrival: it is presented to the API, and only a 200
        /// response establishes the internal session.
        /// </summary>
        public async Task<ApiIdentity?> ValidateTokenAsync(string bearerToken)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<ApiIdentity>(Json)
                : null;
        }

        /* ---------------- suppliers (OVC-248) ---------------- */

        public async Task<List<ApiSupplier>> GetSuppliersAsync(string? status = null)
        {
            Authorise();
            var url = string.IsNullOrWhiteSpace(status) || status == "All"
                ? "api/suppliers"
                : $"api/suppliers?status={Uri.EscapeDataString(status)}";
            return await _http.GetFromJsonAsync<List<ApiSupplier>>(url, Json) ?? new();
        }

        public async Task<ApiSupplier?> GetSupplierAsync(int id)
        {
            Authorise();
            var res = await _http.GetAsync($"api/suppliers/{id}");
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<ApiSupplier>(Json) : null;
        }

        /* ---------------- approval workflow (OVC-249) ---------------- */

        public Task<(bool ok, string message)> ReviewAsync(int id)  => Post($"api/suppliers/{id}/review", null);
        public Task<(bool ok, string message)> ApproveAsync(int id) => Post($"api/suppliers/{id}/approve", null);
        public Task<(bool ok, string message)> RejectAsync(int id, string? reason)
            => Post($"api/suppliers/{id}/reject", new { reason });

        private async Task<(bool ok, string message)> Post(string url, object? body)
        {
            Authorise();
            var res = body is null
                ? await _http.PostAsync(url, null)
                : await _http.PostAsJsonAsync(url, body);

            var payload = await res.Content.ReadFromJsonAsync<ApiDecision>(Json);
            return (res.IsSuccessStatusCode,
                    payload?.Message ?? (res.IsSuccessStatusCode ? "Done." : $"The API returned {(int)res.StatusCode}."));
        }
    }
}
