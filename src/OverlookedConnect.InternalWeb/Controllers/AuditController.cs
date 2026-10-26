using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Filters;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Services;
using System.Text.Json;
using System.Text;

namespace OverlookedConnect.Internal.Controllers;

[RequireRole("executive", "hr")]
public class AuditController : Controller
{
    private readonly OverlookedApiClient _api;

    public AuditController(OverlookedApiClient api)
    {
        _api = api;
    }

private static string FormatAuditValue(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "—";

    try
    {
        using var document = JsonDocument.Parse(value);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return value;

        return string.Join(
            Environment.NewLine,
            document.RootElement.EnumerateObject()
                .Select(property =>
                    $"{property.Name}: {FormatJsonElement(property.Value)}"));
    }
    catch (JsonException)
    {
        return value;
    }
}

private static string FormatJsonElement(JsonElement element)
{
    return element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Null => "—",
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        _ => element.ToString()
    };
}

    [HttpGet]
    public async Task<IActionResult> Index(
        string? filter,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 20;

        page = Math.Max(1, page);

        ViewBag.ActionFilter = filter ?? "All actions";
        ViewBag.CurrentPage = page;

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
            var result = await _api.GetAuditLogsAsync(
                filter,
                page,
                pageSize,
                accessToken,
                cancellationToken);

            var entries = result.Entries.Select(e => new AuditEntry
            {
                Timestamp = e.Timestamp.ToLocalTime()
                    .ToString("dd MMM yyyy HH:mm:ss"),

                User = e.UserName,
                Role = e.Role,
                Action = e.Action,
                Entity = $"{e.EntityName} #{e.EntityId}",
                OldValue = FormatAuditValue(e.OldValue),
                NewValue = FormatAuditValue(e.NewValue)
            }).ToList();

            ViewBag.TotalEntries = result.Total;
            ViewBag.TotalPages = (int)Math.Ceiling(
                (double)result.Total / pageSize);

            return View(entries);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] =
                "Your session has expired or you are not authorised.";

            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException)
        {
            ViewBag.ErrorMessage =
                "Unable to retrieve audit records from the API.";

            return View(new List<AuditEntry>());
        }
    }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Export(
    string? filter,
    CancellationToken cancellationToken)
{
    var accessToken = HttpContext.Session.GetString("AccessToken");

    if (string.IsNullOrWhiteSpace(accessToken))
    {
        TempData["ErrorMessage"] =
            "Your session has expired. Please sign in again.";

        return RedirectToAction("Login", "Account");
    }

    const int pageSize = 100;
    const int maxRecords = 10000;

    try
    {
        var records = new List<ApiAuditEntry>();
        var page = 1;

        while (true)
        {
            var result = await _api.GetAuditLogsAsync(
                filter,
                page,
                pageSize,
                accessToken,
                cancellationToken);

            if (result.Total > maxRecords)
            {
                TempData["ErrorMessage"] =
                    "This export exceeds the 10,000-record limit. " +
                    "Please narrow the action filter.";

                return RedirectToAction("Index", new { filter });
            }

            records.AddRange(result.Entries);

            if (result.Entries.Count == 0 ||
                records.Count >= result.Total)
            {
                break;
            }

            page++;
        }

        var csv = new StringBuilder();

        csv.AppendLine(
            "Timestamp (UTC),User,Role,Action,Entity,Old Value,New Value");

        foreach (var entry in records)
        {
            var values = new[]
            {
                entry.Timestamp.ToUniversalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss"),
                entry.UserName,
                entry.Role,
                entry.Action,
                $"{entry.EntityName} #{entry.EntityId}",
                entry.OldValue ?? "",
                entry.NewValue ?? ""
            };

            csv.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        var fileName =
            $"overlooked-audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        var bytes = new UTF8Encoding(true)
            .GetBytes(csv.ToString());

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
    catch (UnauthorizedAccessException)
    {
        TempData["ErrorMessage"] =
            "Your session has expired or you are not authorised.";

        return RedirectToAction("Login", "Account");
    }
    catch (HttpRequestException)
    {
        TempData["ErrorMessage"] =
            "Unable to export audit records. Please try again.";

        return RedirectToAction("Index", new { filter });
    }
}

private static string EscapeCsv(string? value)
{
    value ??= "";

    // Prevent spreadsheet formula execution from untrusted audit values.
    if (value.Length > 0 &&
        (value.TrimStart().StartsWith('=') ||
         value.TrimStart().StartsWith('+') ||
         value.TrimStart().StartsWith('-') ||
         value.TrimStart().StartsWith('@')))
    {
        value = "'" + value;
    }

    return "\"" + value.Replace("\"", "\"\"") + "\"";
}

}
