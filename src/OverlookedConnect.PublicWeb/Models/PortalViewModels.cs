using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

/*
 Portal View Models

 These view models represent form data used by the Overlooked Connect
 public-facing website.

 Task 2:
 - Job applications are submitted to the shared API.
 - Supplier registrations are submitted to the shared API.
 - Contact enquiries will be connected to the shared API.
*/

namespace OverlookedConnect.PublicWeb.Models
{
   public class JobApplicationModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "A valid vacancy is required.")]
    public int VacancyId { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(
        150,
        MinimumLength = 2,
        ErrorMessage =
            "Full Name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    [StringLength(256)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Phone Number is required.")]
    [Phone(ErrorMessage = "Invalid Phone Number.")]
    [StringLength(30)]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Target Position is required.")]
    [StringLength(200)]
    public string Position { get; set; } = "";

    [Required(
        ErrorMessage =
            "ID/Passport number is required.")]
    [StringLength(20)]
    public string IdNumber { get; set; } = "";

    [Required(
        ErrorMessage =
            "Highest qualification is required.")]
    [StringLength(200)]
    public string Qualification { get; set; } = "";

    [Required(
        ErrorMessage =
            "Years of relevant experience is required.")]
    [StringLength(50)]
    public string YearsExperience { get; set; } = "";

    [Required(ErrorMessage = "Please upload your CV in PDF format.")]
[DataType(DataType.Upload)]
public IFormFile ResumeFile { get; set; } = null!;

public bool PopiaConsent { get; set; }
}

    public class SupplierRegistrationModel
    {
        // Company details

        [Required(ErrorMessage = "Registered company name is required.")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Registered company name")]
        public string RegisteredCompanyName { get; set; } = "";

        [Required(ErrorMessage = "Company registration number is required.")]
        [StringLength(50)]
        [Display(Name = "Company registration number")]
        public string CompanyRegistrationNumber { get; set; } = "";

        [StringLength(200)]
        [Display(Name = "Trading name")]
        public string? TradingName { get; set; }

        [StringLength(20)]
        [Display(Name = "VAT number")]
        public string? VatNumber { get; set; }

        [Required(ErrorMessage = "B-BBEE level is required.")]
        [Display(Name = "B-BBEE level")]
        public string BbbeeLevel { get; set; } = "";

        [Required(ErrorMessage = "Number of employees is required.")]
        [Range(1, 100000,
            ErrorMessage = "Enter a valid number of employees.")]
        [Display(Name = "Number of employees")]
        public int? NumberOfEmployees { get; set; }

        // Contact person

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string ContactFullName { get; set; } = "";

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100)]
        [Display(Name = "Email address")]
        public string ContactEmail { get; set; } = "";

        [Required(ErrorMessage = "Contact number is required.")]
        [Phone(ErrorMessage = "Invalid contact number.")]
        [StringLength(20)]
        [Display(Name = "Contact number")]
        public string ContactNumber { get; set; } = "";

        /*
         Task 2 (OVC-247):
         The API creates a Supplier account on registration so the
         applicant can sign in and track their own application.
        */
        [Required(
            ErrorMessage = "Choose a password so you can track your application.")]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters.")]
        [Display(Name = "Choose a password")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Province is required.")]
        [StringLength(50)]
        public string Province { get; set; } = "";

        [Required(
            ErrorMessage = "Please describe the goods or services you offer.")]
        [StringLength(500, MinimumLength = 5)]
        [Display(Name = "Goods / services offered")]
        public string GoodsServicesOffered { get; set; } = "";

        // Documents & consent

        [Required(
            ErrorMessage = "Please upload mandatory compliance documents.")]
        [DataType(DataType.Upload)]
        public IFormFile ComplianceDocs { get; set; } = null!;

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "You must accept the POPIA consent to proceed.")]
        public bool PopiaConsent { get; set; }
    }

    public class ContactEnquiryModel
    {
        [Required(
            ErrorMessage = "Please select a reason for your enquiry.")]
        public string Reason { get; set; } = "Investment";

        [Required(ErrorMessage = "Your name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string Name { get; set; } = "";

        [StringLength(150)]
        public string? Organisation { get; set; }

        [Required(ErrorMessage = "Your email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Invalid contact number.")]
        [StringLength(20)]
        [Display(Name = "Contact number")]
        public string? ContactNumber { get; set; }

        [Required(ErrorMessage = "Message body cannot be empty.")]
        [StringLength(1000, MinimumLength = 10,
            ErrorMessage = "Message must be between 10 and 1000 characters.")]
        public string Message { get; set; } = "";
    }

    /*
     Staff sign-in from the public website (OVC-267).
     Credentials are posted to the shared API.
    */
    public class StaffLoginModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 6)]
        public string Password { get; set; } = "";
    }

    /*
     Carries the issued token to the internal platform's SSO endpoint.
     The token is posted rather than placed in the URL.
    */
    public class StaffHandoffModel
    {
        public string PostUrl { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
    }

    /*
     Read-only view of a supplier's own application.
     Populated from GET /api/suppliers/mine.
    */
    public class SupplierStatusModel
    {
        public string Reference { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Status { get; set; } = "";
        public string? VendorNumber { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int DocumentsVerified { get; set; }
        public int DocumentsRequired { get; set; }

        public List<(string DocType, string Status)> Documents { get; set; }
            = new();
    }
}