using System.ComponentModel.DataAnnotations;

namespace OverlookedConnect.PublicWeb.Models;

public class ApplicantRegisterModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(
        128,
        MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class ApplicantLoginModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class ApplicantDashboardModel
{
    public string ApplicantName { get; set; } = "";

    public List<ApplicantApplicationModel> Applications { get; set; } = new();
}

public class ApplicantApplicationModel
{
    public int ApplicationId { get; set; }

    public int VacancyId { get; set; }

    public string VacancyTitle { get; set; } = "";

    public string Department { get; set; } = "";

    public string Location { get; set; } = "";

    public string ApplicantName { get; set; } = "";

    public string ApplicantEmail { get; set; } = "";

    public string PhoneNumber { get; set; } = "";

    public string IdNumber { get; set; } = "";

    public string Qualification { get; set; } = "";

    public string YearsExperience { get; set; } = "";

    public string Status { get; set; } = "";

    public DateTime SubmittedAt { get; set; }

    public string Reference { get; set; } = "";

    public bool CanEdit { get; set; }

    public bool CanWithdraw { get; set; }
}

public class ApplicantEditApplicationModel
{
    public int ApplicationId { get; set; }

    public string VacancyTitle { get; set; } = "";

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full name")]
    public string ApplicantName { get; set; } = "";

    [Required(ErrorMessage = "Contact number is required.")]
    [StringLength(30)]
    [Display(Name = "Contact number")]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "Qualification is required.")]
    [StringLength(200)]
    public string Qualification { get; set; } = "";

    [Required(ErrorMessage = "Years of experience is required.")]
    [StringLength(50)]
    [Display(Name = "Years of experience")]
    public string YearsExperience { get; set; } = "";
}