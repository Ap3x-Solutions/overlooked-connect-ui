using Microsoft.AspNetCore.Mvc;
using OverlookedConnect.PublicWeb.Models;
using OverlookedConnect.PublicWeb.Services;

namespace OverlookedConnect.PublicWeb.Controllers;

public class ApplicantPortalController : Controller
{
    private readonly OverlookedApiClient _apiClient;

    private const string ApplicantTokenKey = "ApplicantAccessToken";
    private const string ApplicantNameKey = "ApplicantName";
    private const string ApplicantEmailKey = "ApplicantEmail";

    public ApplicantPortalController(
        OverlookedApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // ---------------------------------------------------------
    // REGISTER
    // ---------------------------------------------------------

    [HttpGet]
    public IActionResult Register()
    {
        if (IsApplicantSignedIn())
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new ApplicantRegisterModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        ApplicantRegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, result, message) =
            await _apiClient.RegisterApplicantAsync(
                model.FullName,
                model.Email,
                model.Password,
                model.ConfirmPassword);

        if (!ok)
        {
            ModelState.AddModelError(
                string.Empty,
                message);

            return View(model);
        }

        TempData["SuccessMessage"] =
            message;

        return RedirectToAction(nameof(Login));
    }

    // ---------------------------------------------------------
    // LOGIN
    // ---------------------------------------------------------

    [HttpGet]
    public IActionResult Login()
    {
        if (IsApplicantSignedIn())
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new ApplicantLoginModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        ApplicantLoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await _apiClient.ApplicantLoginAsync(
                model.Email,
                model.Password);

        if (result is null ||
            string.IsNullOrWhiteSpace(result.AccessToken))
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email address or password.");

            return View(model);
        }

        HttpContext.Session.SetString(
            ApplicantTokenKey,
            result.AccessToken);

        HttpContext.Session.SetString(
            ApplicantNameKey,
            result.FullName ?? "");

        HttpContext.Session.SetString(
            ApplicantEmailKey,
            result.Email ?? "");

        TempData["SuccessMessage"] =
            $"Welcome back, {result.FullName}.";

        return RedirectToAction(nameof(Dashboard));
    }

    // ---------------------------------------------------------
    // DASHBOARD / MY APPLICATIONS
    // ---------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var token = GetApplicantToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        var applications =
            await _apiClient.GetMyApplicationsAsync(token);

        var model = new ApplicantDashboardModel
        {
            ApplicantName =
                HttpContext.Session.GetString(
                    ApplicantNameKey) ?? "Applicant",

            Applications =
                applications
                    .Select(MapApplication)
                    .ToList()
        };

        return View(model);
    }

    // ---------------------------------------------------------
    // APPLICATION DETAILS
    // ---------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Application(int id)
    {
        var token = GetApplicantToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        var application =
            await _apiClient.GetMyApplicationAsync(
                id,
                token);

        if (application is null)
        {
            TempData["ErrorMessage"] =
                "The application could not be found.";

            return RedirectToAction(nameof(Dashboard));
        }

        return View(
            MapApplication(application));
    }

    // ---------------------------------------------------------
    // EDIT APPLICATION
    // ---------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var token = GetApplicantToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        var application =
            await _apiClient.GetMyApplicationAsync(
                id,
                token);

        if (application is null)
        {
            TempData["ErrorMessage"] =
                "The application could not be found.";

            return RedirectToAction(nameof(Dashboard));
        }

        if (!application.CanEdit)
        {
            TempData["ErrorMessage"] =
                "This application can no longer be edited.";

            return RedirectToAction(
                nameof(Application),
                new { id });
        }

        var model =
            new ApplicantEditApplicationModel
            {
                ApplicationId =
                    application.ApplicationId,

                VacancyTitle =
                    application.VacancyTitle,

                ApplicantName =
                    application.ApplicantName,

                PhoneNumber =
                    application.PhoneNumber,

                Qualification =
                    application.Qualification,

                YearsExperience =
                    application.YearsExperience
            };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ApplicantEditApplicationModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = GetApplicantToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        var payload =
            new ApiApplicantUpdateRequest(
                model.ApplicantName,
                model.PhoneNumber,
                model.Qualification,
                model.YearsExperience);

        var (ok, message) =
            await _apiClient.UpdateMyApplicationAsync(
                model.ApplicationId,
                payload,
                token);

        if (!ok)
        {
            ModelState.AddModelError(
                string.Empty,
                message);

            return View(model);
        }

        TempData["SuccessMessage"] =
            message;

        return RedirectToAction(
            nameof(Application),
            new
            {
                id = model.ApplicationId
            });
    }

    // ---------------------------------------------------------
    // WITHDRAW APPLICATION
    // ---------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var token = GetApplicantToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        var (ok, message) =
            await _apiClient.WithdrawMyApplicationAsync(
                id,
                token);

        if (!ok)
        {
            TempData["ErrorMessage"] =
                message;

            return RedirectToAction(
                nameof(Application),
                new { id });
        }

        TempData["SuccessMessage"] =
            message;

        return RedirectToAction(nameof(Dashboard));
    }

    // ---------------------------------------------------------
    // LOGOUT
    // ---------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        ClearApplicantSession();

        TempData["SuccessMessage"] =
            "You have been signed out.";

        return RedirectToAction(
            "Careers",
            "Home");
    }

    // ---------------------------------------------------------
    // SESSION HELPERS
    // ---------------------------------------------------------

    private string? GetApplicantToken()
    {
        return HttpContext.Session.GetString(
            ApplicantTokenKey);
    }

    private bool IsApplicantSignedIn()
    {
        return !string.IsNullOrWhiteSpace(
            GetApplicantToken());
    }

    private void ClearApplicantSession()
    {
        HttpContext.Session.Remove(
            ApplicantTokenKey);

        HttpContext.Session.Remove(
            ApplicantNameKey);

        HttpContext.Session.Remove(
            ApplicantEmailKey);
    }

    // ---------------------------------------------------------
    // API -> UI MODEL MAPPING
    // ---------------------------------------------------------

    private static ApplicantApplicationModel MapApplication(
        ApiApplicantApplication application)
    {
        return new ApplicantApplicationModel
        {
            ApplicationId =
                application.ApplicationId,

            VacancyId =
                application.VacancyId,

            VacancyTitle =
                application.VacancyTitle,

            Department =
                application.Department,

            Location =
                application.Location,

            ApplicantName =
                application.ApplicantName,

            ApplicantEmail =
                application.ApplicantEmail,

            PhoneNumber =
                application.PhoneNumber,

            IdNumber =
                application.IdNumber,

            Qualification =
                application.Qualification,

            YearsExperience =
                application.YearsExperience,

            Status =
                application.Status,

            SubmittedAt =
                application.SubmittedAt,

            Reference =
                application.Reference,

            CanEdit =
                application.CanEdit,

            CanWithdraw =
                application.CanWithdraw
        };
    }
}