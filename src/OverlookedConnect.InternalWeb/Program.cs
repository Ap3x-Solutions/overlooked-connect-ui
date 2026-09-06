using OverlookedConnect.Internal.Services;

/*
 Application entry point for the Overlooked Connect internal operations platform.

 Task 2 additions (OVC-267):
   - A typed OverlookedApiClient registered through IHttpClientFactory, pointing at the shared REST
     API, with the bearer token supplied from session.
   - IHttpContextAccessor, so the client can read that token without every controller passing it.

 Session was already enabled for the demonstration role. It now also holds the JWT issued by the
 API, either from a direct sign-in here or from the single sign-on handover initiated on the public
 website.

 Reference List:
    - Microsoft Learn. [s.a.]. Make HTTP requests using IHttpClientFactory in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Session and state management in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Use HttpContext in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context> [Accessed 5 September 2026].
*/

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session services
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

/* OVC-267: typed client for the shared REST API. IHttpContextAccessor lets it read the bearer
   token from session, so no controller handles the token directly. */
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<OverlookedApiClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7039/");
    c.Timeout = TimeSpan.FromSeconds(20);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days.
    // You may want to change this for production scenarios.
    // See https://aka.ms/aspnetcore-hsts
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Enable session
app.UseSession();

// Keep existing authentication configuration
app.UseAuthentication();

app.UseAuthorization();

// Default route
// Opens the InternalWeb application on Account/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
