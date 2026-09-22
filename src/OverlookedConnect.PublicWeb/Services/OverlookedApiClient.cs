using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

/*
 Typed HTTP client for the shared Overlooked Connect REST API.

 Registered with AddHttpClient in Program.cs so HttpClient instances are pooled by
 IHttpClientFactory rather than constructed per request, which avoids socket exhaustion under load
 (Section 7.3). The base address comes from configuration, so moving from localhost to Azure App
 Service is a settings change and not a code change.

 The record types below mirror the API's DTOs exactly. Keeping them in one file means a contract
 change on the API side produces one compile error here rather than several scattered ones.

 Reference List:
    - Microsoft Learn. [s.a.]. Make HTTP requests using IHttpClientFactory in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. HttpClientJsonExtensions Class. [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.json.httpclientjsonextensions> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Configuration in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/> [Accessed 5 September 2026].
*/

namespace OverlookedConnect.PublicWeb.Services
{
    /* ---------- shapes returned by the API ---------- */

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

    public record ApiSupplierRegistrationResult(int SupplierId, string Reference, string Status, string Message);

    public record ApiVacancy(int VacancyId, string Title, string Department, string Site,
        string EmploymentType, string Purpose, List<string> Requirements, DateTime ClosingDate);
public record ApiJobApplicationResponse(
    int ApplicationId,
    int VacancyId,
    string VacancyTitle,
    string ApplicantName,
    string ApplicantEmail,
    string Status,
    DateTime SubmittedAt);
    /* ---------- client ---------- */

    public class OverlookedApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public OverlookedApiClient(HttpClient http) => _http = http;

        /// <summary>
        /// Authenticates against the API. Used by the staff sign-in page on the public site, which
        /// then hands the token to the internal platform (OVC-267).
        /// Returns null on any non-success response, so the caller shows one generic message and
        /// the page cannot be used to discover which addresses are registered.
        /// </summary>
        public async Task<ApiLoginResponse?> LoginAsync(string email, string password)
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            return res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<ApiLoginResponse>(Json)
                : null;
        }

        /// <summary>OVC-247 — public supplier registration (Figure 20).</summary>
        public async Task<(bool ok, ApiSupplierRegistrationResult? result, string message)> RegisterSupplierAsync(object payload)
        {
            var res = await _http.PostAsJsonAsync("api/suppliers", payload);

            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiSupplierRegistrationResult>(Json);
                return (true, body, body?.Message ?? "Application received.");
            }

            var err = await res.Content.ReadFromJsonAsync<ApiMessage>(Json);
            return (false, null, err?.Message ?? $"Registration could not be completed ({(int)res.StatusCode}).");
        }

        /// <summary>The signed-in supplier's own application, for the status tracker (Figure 22).</summary>
        public async Task<ApiSupplier?> GetMySupplierAsync(string bearerToken)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "api/suppliers/mine");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<ApiSupplier>(Json) : null;
        }

        /// <summary>OVC-250 — active vacancies for the careers page. Anonymous endpoint.</summary>
        public async Task<List<ApiVacancy>> GetVacanciesAsync()
            => await _http.GetFromJsonAsync<List<ApiVacancy>>("api/vacancies", Json) ?? new();

        public async Task<ApiVacancy?> GetVacancyAsync(int id)
        {
            var res = await _http.GetAsync($"api/vacancies/{id}");
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<ApiVacancy>(Json) : null;
        }
        public async Task<(bool ok, ApiJobApplicationResponse? result, string message)>
    SubmitJobApplicationAsync(
        int vacancyId,
        string applicantName,
        string applicantEmail,
        string phoneNumber,
        string idNumber,
        string qualification,
        string yearsExperience,
        bool popiaConsent,
        IFormFile resumeFile)
{
    using var content = new MultipartFormDataContent();

    content.Add(
        new StringContent(vacancyId.ToString()),
        "VacancyId");

    content.Add(
        new StringContent(applicantName),
        "ApplicantName");

    content.Add(
        new StringContent(applicantEmail),
        "ApplicantEmail");

    content.Add(
        new StringContent(phoneNumber),
        "PhoneNumber");

    content.Add(
        new StringContent(idNumber),
        "IdNumber");

    content.Add(
        new StringContent(qualification),
        "Qualification");

    content.Add(
        new StringContent(yearsExperience),
        "YearsExperience");

    content.Add(
        new StringContent(popiaConsent.ToString().ToLowerInvariant()),
        "PopiaConsent");

    await using var stream = resumeFile.OpenReadStream();

    using var fileContent = new StreamContent(stream);

    fileContent.Headers.ContentType =
        new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(resumeFile.ContentType)
                ? "application/pdf"
                : resumeFile.ContentType);

    content.Add(
        fileContent,
        "ResumeFile",
        resumeFile.FileName);

    var response = await _http.PostAsync(
        "api/job-applications",
        content);

    if (response.IsSuccessStatusCode)
    {
        var result =
            await response.Content
                .ReadFromJsonAsync<ApiJobApplicationResponse>(Json);

        return (
            true,
            result,
            "Your job application has been successfully submitted.");
    }

    try
    {
        var error =
            await response.Content
                .ReadFromJsonAsync<ApiMessage>(Json);

        return (
            false,
            null,
            error?.Message ??
            $"Your application could not be submitted ({(int)response.StatusCode}).");
    }
    catch
    {
        return (
            false,
            null,
            $"Your application could not be submitted ({(int)response.StatusCode}).");
    }
}
    }
}
