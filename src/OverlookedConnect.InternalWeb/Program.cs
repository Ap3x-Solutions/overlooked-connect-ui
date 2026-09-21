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