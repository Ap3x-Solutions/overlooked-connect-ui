
using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Filters;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;

namespace OverlookedConnect.Internal.Controllers;

[RequireRole("executive", "hr")]
public class ReportsController : Controller
{
    private readonly OverlookedApiClient _api;

    public ReportsController(OverlookedApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ReportRequestModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        ReportRequestModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var supportedReports = new[]
        {
            "Safety incident summary",
            "Workforce headcount",
            "Leave register"
        };

        if (!supportedReports.Contains(
                model.ReportType,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.ReportType),
                "The selected report is not available.");

            return View("Index", model);
        }

        if (!string.Equals(
                model.Format,
                "CSV",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.Format),
                "Only CSV export is currently supported.");

            return View("Index", model);
        }

        var accessToken =
            HttpContext.Session.GetString("AccessToken");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            TempData["ErrorMessage"] =
                "Your session has expired. Please sign in again.";

            return RedirectToAction("Login", "Account");
        }
try
{
    using var response = await _api.ExportReportAsync(
        model.ReportType,
        model.BusinessUnit,
        accessToken,
        cancellationToken);

    if (response.StatusCode ==
        System.Net.HttpStatusCode.Unauthorized)
    {
        TempData["ErrorMessage"] =
            "Your session has expired. Please sign in again.";

        return RedirectToAction("Login", "Account");
    }

    if (response.StatusCode ==
        System.Net.HttpStatusCode.Forbidden)
    {
        ModelState.AddModelError(
            string.Empty,
            "You do not have permission to export this report.");

        return View("Index", model);
    }

    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadAsByteArrayAsync(
        cancellationToken);

    var fileName =
        response.Content.Headers.ContentDisposition?.FileNameStar
        ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
        ?? "overlooked-report.csv";

    return File(
        content,
        "text/csv; charset=utf-8",
        fileName);
}
catch (HttpRequestException)
{
    ModelState.AddModelError(
        string.Empty,
        "The report could not be downloaded. Please try again.");

    return View("Index", model);
}

        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] =
                "Your session has expired or you are not authorised.";

            return RedirectToAction("Index");
        }
    }
}