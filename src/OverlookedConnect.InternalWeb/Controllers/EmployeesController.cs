using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.Internal.Models;
using OverlookedConnect.Internal.Filters;

/*
 This controller serves the Employee Management module (FR-02, US-03), used by the HR & Operations role.
 It supports searching and filtering the employee database by business unit, job title and employment
 status, which is the requirement recorded in Section 2.2 of the Task 1 documentation.

 Filtering is performed in memory against the DemoData store. In Task 2 this becomes a parameterised
 query through ILeaveRepository / IEmployeeRepository against Azure SQL Database, so that the query is
 never built by string concatenation and SQL injection is structurally prevented (Section 8.3).
 */

namespace OverlookedConnect.Internal.Controllers
{
    [RequireRole("hr", "executive")]
    public sealed class EmployeesController : Controller
    {
        /* GET: Employees/Index */
        public IActionResult Index(string? businessUnit, string? status, string? search)
        {
            var employees = DemoData.Employees.AsEnumerable(); /* [Microsoft Learn, [s.a.]] */

            if (!string.IsNullOrWhiteSpace(businessUnit) && businessUnit != "All units")
            {
                employees = employees.Where(e => e.BusinessUnit == businessUnit);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                employees = employees.Where(e => e.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                employees = employees.Where(e =>
                    e.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.EmployeeId.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.BusinessUnit = businessUnit ?? "Mining Operations";
            ViewBag.Status = status ?? "Active";
            ViewBag.Search = search;

            return View(employees.ToList());
        }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Enumerable.Where Method (System.Linq). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.where> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Language Integrated Query (LINQ) in C#. [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/linq/> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. ViewBag, ViewData and TempData in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview> [Accessed 14 August 2026].
*/
