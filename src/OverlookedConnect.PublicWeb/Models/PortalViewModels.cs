using System.ComponentModel.DataAnnotations; /* [Microsoft Learn, [s.a.]] */
using Microsoft.AspNetCore.Http;

/* 
 Portal View Models 
 This file contains the view models used in the OverlookedConnect.PublicWeb public facing website interface, specifically the forms. 
 View models are classes that represent the data and behavior of the views in the application. 
 They are used to transfer data between the controller and the view, and to encapsulate the logic needed to display the data in a user-friendly way.
 The purpose of the Views is to provide a user interface for the webiste Job application, supplier registration, and contact enquiry models, allowing users to interact with the data and perform actions such as adding records to the Overlooked database.
 Unfortuantely, they are only used as valdation models for the forms, with alarms/error messages displayed on the form, and no actual data stored. They will be implemented later in Task 2.
 */

namespace OverlookedConnect.PublicWeb.Models
{
    public class JobApplicationModel /* [Microsoft Learn, [s.a.]] */
    {
        [Required(ErrorMessage = "Full Name is required.")] /* [Microsoft Learn, [s.a.]] */
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        [StringLength(15)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Target Position is required.")]
        public string Position { get; set; }

        [Required(ErrorMessage = "ID/Passport number is required.")]
        [StringLength(20)]
        public string IdNumber { get; set; }

        [Required(ErrorMessage = "Highest qualification is required.")]
        public string Qualification { get; set; }

        [Required(ErrorMessage = "Years of relevant experience is required.")]
        public string YearsExperience { get; set; }

        [Required(ErrorMessage = "Please upload your CV in PDF format.")]
        [DataType(DataType.Upload)]
        public IFormFile ResumeFile { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the POPIA consent to proceed.")]
        public bool PopiaConsent { get; set; }
    }

    public class SupplierRegistrationModel
    {
        // Company details
        [Required(ErrorMessage = "Registered company name is required.")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Registered company name")]
        public string RegisteredCompanyName { get; set; }

        [Required(ErrorMessage = "Company registration number is required.")]
        [StringLength(50)]
        [Display(Name = "Company registration number")]
        public string CompanyRegistrationNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Trading name")]
        public string? TradingName { get; set; }

        [StringLength(20)]
        [Display(Name = "VAT number")]
        public string? VatNumber { get; set; }

        [Required(ErrorMessage = "B-BBEE level is required.")]
        [Display(Name = "B-BBEE level")]
        public string BbbeeLevel { get; set; }

        [Required(ErrorMessage = "Number of employees is required.")]
        [Range(1, 100000, ErrorMessage = "Enter a valid number of employees.")]
        [Display(Name = "Number of employees")]
        public int? NumberOfEmployees { get; set; }

        // Contact person
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string ContactFullName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100)]
        [Display(Name = "Email address")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [Phone(ErrorMessage = "Invalid contact number.")]
        [StringLength(20)]
        [Display(Name = "Contact number")]
        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Province is required.")]
        [StringLength(50)]
        public string Province { get; set; }

        [Required(ErrorMessage = "Please describe the goods or services you offer.")]
        [StringLength(500, MinimumLength = 5)]
        [Display(Name = "Goods / services offered")]
        public string GoodsServicesOffered { get; set; }

        // Documents & consent
        [Required(ErrorMessage = "Please upload mandatory compliance documents.")]
        [DataType(DataType.Upload)]
        public IFormFile ComplianceDocs { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the POPIA consent to proceed.")]
        public bool PopiaConsent { get; set; }
    }

    public class ContactEnquiryModel
    {
        [Required(ErrorMessage = "Please select a reason for your enquiry.")]
        public string Reason { get; set; } = "Investment";

        [Required(ErrorMessage = "Your name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string Name { get; set; }

        [StringLength(150)]
        public string? Organisation { get; set; }

        [Required(ErrorMessage = "Your email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100)]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid contact number.")]
        [StringLength(20)]
        [Display(Name = "Contact number")]
        public string? ContactNumber { get; set; }

        [Required(ErrorMessage = "Message body cannot be empty.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 1000 characters.")]
        public string Message { get; set; }
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Model Binding in ASP.NET Core. [online]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding> [14 August 2026].
        - Microsoft Learn. [s.a.]. System.ComponentModel.DataAnnotations Namespace. [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations> [14 August 2026].
        - Microsoft Learn. [s.a.]. RequiredAttribute Class. [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute> [14 August 2026].
*/