using OverlookedConnect.PublicWeb.Services;

/*
 Application entry point for the Overlooked Connect public website.

 Task 2 additions (OVC-267):
   - A typed OverlookedApiClient registered through IHttpClientFactory, pointing at the shared REST
     API. The base address comes from configuration so moving to Azure is a settings change.
   - Session state, used to hold a signed-in supplier's token for the status tracker. The cookie is
     HttpOnly, so the token is never readable by client-side script.

 Reference List:
    - Microsoft Learn. [s.a.]. Make HTTP requests using IHttpClientFactory in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. Session and state management in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state> [Accessed 5 September 2026].
    - Microsoft Learn. [s.a.]. App startup in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup> [Accessed 5 September 2026].
*/

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

/* OVC-267: typed client for the shared REST API */
builder.Services.AddHttpClient<OverlookedApiClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7039/");
    c.Timeout = TimeSpan.FromSeconds(20);
});

/* Session holds a signed-in supplier's token so the status tracker can load their application.
   HttpOnly keeps it out of reach of any injected script (Section 8.1). */
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
