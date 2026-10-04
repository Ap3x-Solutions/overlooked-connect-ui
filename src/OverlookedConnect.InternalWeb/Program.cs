/*
 * Overlooked Connect - Internal Operations Web Application
 *
 * ASP.NET Core MVC is used to provide the presentation layer for the
 * internal operations portal. Session state stores authenticated user
 * context between HTTP requests, while the session cookie is configured
 * as HttpOnly to reduce exposure to client-side scripts (Microsoft, [s.a.]a).
 *
 * IHttpClientFactory is used to configure the typed OverlookedApiClient
 * that communicates with the shared Overlooked Connect REST API
 * (Microsoft, [s.a.]b).
 *
 * HTTPS redirection and HTTP Strict Transport Security (HSTS) are enabled
 * for secure production communication (Microsoft, [s.a.]c).
 *
 * References:
 * Microsoft. [s.a.]a. Session and state management in ASP.NET Core.
 * [online]. Available at:
 * <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state>
 * [Accessed 4 October 2026].
 *
 * Microsoft. [s.a.]b. Make HTTP requests using IHttpClientFactory in
 * ASP.NET Core. [online]. Available at:
 * <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests>
 * [Accessed 4 October 2026].
 *
 * Microsoft. [s.a.]c. Enforce HTTPS in ASP.NET Core.
 * [online]. Available at:
 * <https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl>
 * [Accessed 4 October 2026].
 */

using OverlookedConnect.Internal.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// MVC
// ------------------------------------------------------------

builder.Services.AddControllersWithViews();


// ------------------------------------------------------------
// SESSION
// ------------------------------------------------------------

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.Name =
        ".OverlookedConnect.Internal.Session";

    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.SameAsRequest;
});


// ------------------------------------------------------------
// OVERLOOKED CONNECT API
// ------------------------------------------------------------

var apiBaseUrl =
    builder.Configuration["Api:BaseUrl"];

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "Api:BaseUrl is not configured.");
}

if (!apiBaseUrl.EndsWith('/'))
{
    apiBaseUrl += "/";
}

builder.Services.AddHttpClient<OverlookedApiClient>(
    client =>
    {
        client.BaseAddress =
            new Uri(apiBaseUrl);

        client.Timeout =
            TimeSpan.FromSeconds(30);

        client.DefaultRequestHeaders.Accept.Clear();

        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                "application/json"));
    });


// ------------------------------------------------------------
// APPLICATION
// ------------------------------------------------------------

var app = builder.Build();


// ------------------------------------------------------------
// ERROR HANDLING
// ------------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Account/Error");

    app.UseHsts();
}


// ------------------------------------------------------------
// HTTP PIPELINE
// ------------------------------------------------------------

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();


// ------------------------------------------------------------
// ROUTING
// ------------------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Login}/{id?}");

app.Run();
