/*
 Demo Data Store
 This file holds the in-memory sample data used by the Overlooked Connect Internal Operations Platform
 prototype. It exists so that every screen can demonstrate realistic flow of information without a
 database being attached, which is the requirement for Task 1 (design and interaction only).

 The dummy data is deliberately consistent across screens so that a single record can be followed
 through the system, as documented in Section 2.5.3 of the Task 1 documentation:
   - Employee S. Dlamini (EMP-1042) submits a leave request, which appears in the Approvals Queue,
     is declined in Leave Management because of a roster clash, and shows as a warning on the Shift Roster.
   - Incident INC-1042 is captured by the same employee on the staff application, then progresses
     through the Safety register and finally appears in the Audit Log.
   - Supplier #482 (Bethal Logistics CC, OVL-SUP-2026-0482) runs through the Supplier Review Queue
     and is recorded in the Audit Log.

 In Task 2 this static class will be replaced by repository classes backed by Entity Framework Core
 and Azure SQL Database, as described in Section 6.1 and Section 7.1 of the documentation.
 */

namespace OverlookedConnect.Internal.Models
{
    /* ---------------- Domain records ---------------- */

    public class Employee
    {
        public string EmployeeId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Initials { get; set; } = "";
        public string BusinessUnit { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string Site { get; set; } = "";
        public decimal LeaveBalance { get; set; }
        public string Status { get; set; } = "Active";
        public string AvatarColour { get; set; } = "#2C4A7A";
    }

    public class LeaveRequest
    {
        public int LeaveId { get; set; }
        public string EmployeeId { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Initials { get; set; } = "";
        public string AvatarColour { get; set; } = "#2C4A7A";
        public string LeaveType { get; set; } = "";
        public string Dates { get; set; } = "";
        public decimal Days { get; set; }
        public decimal BalanceAfter { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public bool RosterClash { get; set; }
        public string? ClashDetail { get; set; }
    }

    public class ShiftEntry
    {
        public string EmployeeName { get; set; } = "";
        public string Initials { get; set; } = "";
        public string AvatarColour { get; set; } = "#2C4A7A";
        /* Seven values, Monday to Sunday: Day, Night, Rest or Leave */
        public string[] Pattern { get; set; } = Array.Empty<string>();
    }

    public class Incident
    {
        public string IncidentId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Site { get; set; } = "";
        public string Severity { get; set; } = "";
        public string InvestigationStatus { get; set; } = "";
        public string ReportedBy { get; set; } = "";
        public string DateReported { get; set; } = "";
        public string Description { get; set; } = "";
        public int PhotoCount { get; set; }
        /* Index of the current stage in the lifecycle described in Section 5.2.2 */
        public int LifecycleStage { get; set; }
    }

    public class SupplierApplication
    {
        public int SupplierId { get; set; }
        public string CompanyName { get; set; } = "";
        public string Initials { get; set; } = "";
        public string AvatarColour { get; set; } = "#2C4A7A";
        public string Reference { get; set; } = "";
        public string RegistrationNumber { get; set; } = "";
        public string BbbeeLevel { get; set; } = "";
        public int DocumentsVerified { get; set; }
        public int DocumentsRequired { get; set; } = 5;
        public int Employees { get; set; }
        public string Province { get; set; } = "";
        public string Category { get; set; } = "";
        public string Submitted { get; set; } = "";
        public string Status { get; set; } = "Under review";
        public List<ComplianceDocument> Documents { get; set; } = new();
    }

    public class ComplianceDocument
    {
        public string DocType { get; set; } = "";
        public string Status { get; set; } = "Required";  /* Verified, Pending or Required */
    }

    public class CsrProject
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Budget { get; set; }
        public decimal Spent { get; set; }
        public string Beneficiaries { get; set; } = "";
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = "";
    }

    public class AuditEntry
    {
        public string Timestamp { get; set; } = "";
        public string User { get; set; } = "";
        public string Role { get; set; } = "";
        public string Action { get; set; } = "";
        public string Entity { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }

    public class ApprovalItem
    {
        public string Type { get; set; } = "";
        public string Requester { get; set; } = "";
        public string Initials { get; set; } = "";
        public string AvatarColour { get; set; } = "#2C4A7A";
        public string Department { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Waiting { get; set; } = "";
        public bool Overdue { get; set; }
    }

    /* ---------------- Static demo store ---------------- */

    public static class DemoData
    {
        /* Roles used for the demonstration sign-in and the sidebar user chip. */
        public static readonly Dictionary<string, (string Name, string Initials, string Role)> Users = new()
        {
            ["executive"]   = ("T. Mahlangu", "TM", "Executive"),
            ["hr"]          = ("N. Sithole", "NS", "HR & Operations"),
            ["safety"]      = ("P. Naidoo", "PN", "Safety Officer"),
            ["procurement"] = ("S. Nkosi", "SN", "Procurement"),
            ["employee"]    = ("S. Dlamini", "SD", "Employee")
        };

        public static List<Employee> Employees => new()
        {
            new() { EmployeeId = "EMP-1042", FullName = "Sipho Dlamini",    Initials = "SD", BusinessUnit = "Mining Operations", JobTitle = "Shift Supervisor", Site = "Forzando South",     LeaveBalance = 12.5m, Status = "Active",    AvatarColour = "#2C4A7A" },
            new() { EmployeeId = "EMP-1043", FullName = "Precious Naidoo",  Initials = "PN", BusinessUnit = "Mining Operations", JobTitle = "Safety Officer",   Site = "Dorstfontein West",  LeaveBalance = 8.0m,  Status = "Active",    AvatarColour = "#1E6B3A" },
            new() { EmployeeId = "EMP-1044", FullName = "Kagiso Mabaso",    Initials = "KM", BusinessUnit = "Mining Operations", JobTitle = "Fitter",           Site = "Forzando South",     LeaveBalance = 15.0m, Status = "On leave",  AvatarColour = "#8A5A2B" },
            new() { EmployeeId = "EMP-1045", FullName = "Lerato Khumalo",   Initials = "LK", BusinessUnit = "Mining Operations", JobTitle = "Plant Operator",   Site = "Forzando South",     LeaveBalance = 4.5m,  Status = "Active",    AvatarColour = "#4B79B5" },
            new() { EmployeeId = "EMP-1046", FullName = "Johan van Wyk",    Initials = "JW", BusinessUnit = "Mining Operations", JobTitle = "Mine Overseer",    Site = "Overlooked Colliery",LeaveBalance = 21.0m, Status = "Active",    AvatarColour = "#6B3FA0" },
            new() { EmployeeId = "EMP-1047", FullName = "Nomsa Zulu",       Initials = "NZ", BusinessUnit = "Mining Operations", JobTitle = "Diesel Mechanic",  Site = "Forzando North",     LeaveBalance = 9.5m,  Status = "Active",    AvatarColour = "#B8860B" },
            new() { EmployeeId = "EMP-1048", FullName = "Andile Mthembu",   Initials = "AM", BusinessUnit = "Mining Operations", JobTitle = "Blaster",          Site = "Dorstfontein West",  LeaveBalance = 6.0m,  Status = "Suspended", AvatarColour = "#C0392B" },
            new() { EmployeeId = "EMP-1049", FullName = "Refilwe Motsepe",  Initials = "RM", BusinessUnit = "Mining Operations", JobTitle = "Geologist",        Site = "Emalahleni",         LeaveBalance = 18.0m, Status = "Active",    AvatarColour = "#0F766E" }
        };

        public static List<LeaveRequest> LeaveRequests => new()
        {
            new() { LeaveId = 2214, EmployeeId = "EMP-1042", EmployeeName = "Sipho Dlamini",   Initials = "SD", AvatarColour = "#2C4A7A", LeaveType = "Annual",        Dates = "14–18 Aug 2026", Days = 5.0m,  BalanceAfter = 12.5m, ApprovalStatus = "Pending" },
            new() { LeaveId = 2215, EmployeeId = "EMP-1043", EmployeeName = "Precious Naidoo", Initials = "PN", AvatarColour = "#1E6B3A", LeaveType = "Study",         Dates = "03–07 Sep 2026", Days = 5.0m,  BalanceAfter = 8.0m,  ApprovalStatus = "Pending" },
            new() { LeaveId = 2216, EmployeeId = "EMP-1044", EmployeeName = "Kagiso Mabaso",   Initials = "KM", AvatarColour = "#8A5A2B", LeaveType = "Sick",          Dates = "28–29 Jul 2026", Days = 2.0m,  BalanceAfter = 15.0m, ApprovalStatus = "Approved" },
            new() { LeaveId = 2217, EmployeeId = "EMP-1045", EmployeeName = "Lerato Khumalo",  Initials = "LK", AvatarColour = "#4B79B5", LeaveType = "Annual",        Dates = "11–22 Aug 2026", Days = 10.0m, BalanceAfter = 4.5m,  ApprovalStatus = "Declined", RosterClash = true, ClashDetail = "11–22 August overlaps a rostered shift at Forzando South on 14 and 15 August." },
            new() { LeaveId = 2218, EmployeeId = "EMP-1046", EmployeeName = "Johan van Wyk",   Initials = "JW", AvatarColour = "#6B3FA0", LeaveType = "Family resp.",  Dates = "05 Aug 2026",    Days = 1.0m,  BalanceAfter = 21.0m, ApprovalStatus = "Approved" },
            new() { LeaveId = 2219, EmployeeId = "EMP-1047", EmployeeName = "Nomsa Zulu",      Initials = "NZ", AvatarColour = "#B8860B", LeaveType = "Annual",        Dates = "19–23 Aug 2026", Days = 5.0m,  BalanceAfter = 9.5m,  ApprovalStatus = "Approved" }
        };

        public static List<ShiftEntry> Roster => new()
        {
            new() { EmployeeName = "Sipho Dlamini",  Initials = "SD", AvatarColour = "#2C4A7A", Pattern = new[]{ "Day","Day","Day","Night","Night","Rest","Rest" } },
            new() { EmployeeName = "Kagiso Mabaso",  Initials = "KM", AvatarColour = "#8A5A2B", Pattern = new[]{ "Night","Night","Rest","Rest","Day","Day","Day" } },
            new() { EmployeeName = "Lerato Khumalo", Initials = "LK", AvatarColour = "#4B79B5", Pattern = new[]{ "Rest","Rest","Day","Day","Day","Night","Night" } },
            new() { EmployeeName = "Johan van Wyk",  Initials = "JW", AvatarColour = "#6B3FA0", Pattern = new[]{ "Day","Day","Night","Night","Rest","Rest","Day" } },
            new() { EmployeeName = "Nomsa Zulu",     Initials = "NZ", AvatarColour = "#B8860B", Pattern = new[]{ "Leave","Leave","Leave","Day","Day","Night","Night" } },
            new() { EmployeeName = "Andile Mthembu", Initials = "AM", AvatarColour = "#C0392B", Pattern = new[]{ "Night","Rest","Rest","Day","Day","Day","Night" } }
        };

        public static List<Incident> Incidents => new()
        {
            new() { IncidentId = "INC-1042", Title = "Conveyor guard detached",      Site = "Forzando South",      Severity = "High",   InvestigationStatus = "Under investigation", ReportedBy = "S. Dlamini (EMP-1042)", DateReported = "22 Jul 2026, 09:41", PhotoCount = 5, LifecycleStage = 2,
                     Description = "Conveyor guard on CV-04 found detached during routine walk-through. Belt stopped and area barricaded. No injury sustained." },
            new() { IncidentId = "INC-1041", Title = "Slip and fall — wash bay",     Site = "Dorstfontein West",   Severity = "Medium", InvestigationStatus = "Corrective action",   ReportedBy = "P. Naidoo (EMP-1043)",  DateReported = "20 Jul 2026, 14:12", PhotoCount = 2, LifecycleStage = 3,
                     Description = "Employee slipped on standing water in the wash bay. Minor bruising, no lost time. Drainage inspection raised." },
            new() { IncidentId = "INC-1040", Title = "Vehicle near miss — haul road",Site = "Forzando North",      Severity = "Medium", InvestigationStatus = "Verification",       ReportedBy = "J. van Wyk (EMP-1046)", DateReported = "17 Jul 2026, 06:55", PhotoCount = 1, LifecycleStage = 4,
                     Description = "Light vehicle entered the haul road without radio clearance. No contact. Radio protocol retraining assigned." },
            new() { IncidentId = "INC-1039", Title = "Dust exposure exceedance",     Site = "Forzando North",      Severity = "Low",    InvestigationStatus = "Closed",             ReportedBy = "P. Naidoo (EMP-1043)",  DateReported = "09 Jul 2026, 11:30", PhotoCount = 0, LifecycleStage = 5,
                     Description = "Personal dust sampling exceeded the action level for one shift. Ventilation adjusted and re-sampled within limits." },
            new() { IncidentId = "INC-1038", Title = "Hand injury — maintenance",    Site = "Overlooked Colliery", Severity = "Medium", InvestigationStatus = "Closed",             ReportedBy = "N. Zulu (EMP-1047)",    DateReported = "02 Jul 2026, 15:48", PhotoCount = 3, LifecycleStage = 5,
                     Description = "Laceration to the left hand during belt splicing. Treated on site, returned to duty same shift." },
            new() { IncidentId = "INC-1037", Title = "Fall of ground — section 4B",  Site = "Dorstfontein West",   Severity = "High",   InvestigationStatus = "Escalated",          ReportedBy = "A. Mthembu (EMP-1048)", DateReported = "28 Jun 2026, 04:20", PhotoCount = 6, LifecycleStage = 2,
                     Description = "Localised fall of ground in section 4B. Area made safe. Reportable to the DMRE under the Mine Health and Safety Act." }
        };

        public static List<SupplierApplication> Suppliers => new()
        {
            new() { SupplierId = 482, CompanyName = "Bethal Logistics CC",          Initials = "BL", AvatarColour = "#2C4A7A", Reference = "OVL-SUP-2026-0482", RegistrationNumber = "2019/447281/23", BbbeeLevel = "Level 1", DocumentsVerified = 2, Employees = 24, Province = "Mpumalanga", Category = "Bulk haulage & plant hire", Submitted = "18 Jul 2026", Status = "Under review",
                     Documents = new(){ new(){DocType="B-BBEE certificate",Status="Verified"}, new(){DocType="Tax clearance",Status="Verified"}, new(){DocType="CIPC registration",Status="Pending"}, new(){DocType="Public liability",Status="Required"}, new(){DocType="Bank confirmation",Status="Required"} } },
            new() { SupplierId = 481, CompanyName = "Nkosi Plant Hire",             Initials = "NP", AvatarColour = "#1E6B3A", Reference = "OVL-SUP-2026-0481", RegistrationNumber = "2017/338192/07", BbbeeLevel = "Level 2", DocumentsVerified = 5, Employees = 41, Province = "Mpumalanga", Category = "Plant hire",               Submitted = "17 Jul 2026", Status = "Verified" },
            new() { SupplierId = 480, CompanyName = "Mpumalanga Safety Supplies",   Initials = "MS", AvatarColour = "#8A5A2B", Reference = "OVL-SUP-2026-0480", RegistrationNumber = "2020/551204/23", BbbeeLevel = "Level 1", DocumentsVerified = 5, Employees = 12, Province = "Mpumalanga", Category = "PPE & safety equipment",   Submitted = "16 Jul 2026", Status = "Verified" },
            new() { SupplierId = 479, CompanyName = "Highveld Electrical CC",       Initials = "HE", AvatarColour = "#C0392B", Reference = "OVL-SUP-2026-0479", RegistrationNumber = "2015/119883/23", BbbeeLevel = "Level 4", DocumentsVerified = 2, Employees = 9,  Province = "Mpumalanga", Category = "Electrical contracting",   Submitted = "15 Jul 2026", Status = "Awaiting docs" },
            new() { SupplierId = 478, CompanyName = "Tswelo Catering Services",     Initials = "TC", AvatarColour = "#4B79B5", Reference = "OVL-SUP-2026-0478", RegistrationNumber = "2021/667410/07", BbbeeLevel = "Level 1", DocumentsVerified = 5, Employees = 33, Province = "Mpumalanga", Category = "Catering",                 Submitted = "14 Jul 2026", Status = "Under review" },
            new() { SupplierId = 477, CompanyName = "Emalahleni Transport",         Initials = "ET", AvatarColour = "#6B3FA0", Reference = "OVL-SUP-2026-0477", RegistrationNumber = "2018/220945/23", BbbeeLevel = "Level 3", DocumentsVerified = 4, Employees = 58, Province = "Mpumalanga", Category = "Road transport",           Submitted = "12 Jul 2026", Status = "Under review" },
            new() { SupplierId = 476, CompanyName = "Steve Tshwete Cleaning",       Initials = "ST", AvatarColour = "#0F766E", Reference = "OVL-SUP-2026-0476", RegistrationNumber = "2022/884301/07", BbbeeLevel = "Level 1", DocumentsVerified = 5, Employees = 27, Province = "Mpumalanga", Category = "Industrial cleaning",      Submitted = "11 Jul 2026", Status = "Verified" },
            new() { SupplierId = 475, CompanyName = "Komati Engineering Works",     Initials = "KE", AvatarColour = "#B8860B", Reference = "OVL-SUP-2026-0475", RegistrationNumber = "2014/097233/23", BbbeeLevel = "Level 2", DocumentsVerified = 1, Employees = 16, Province = "Mpumalanga", Category = "Fabrication",              Submitted = "09 Jul 2026", Status = "Awaiting docs" }
        };

        public static List<CsrProject> CsrProjects => new()
        {
            new() { Name = "Bethal Skills Centre",     Category = "Skills development",    Budget = 1200000, Spent = 780000, Beneficiaries = "240 learners",    ProgressPercent = 65,  Status = "In progress" },
            new() { Name = "Emalahleni Clinic upgrade",Category = "Health",                Budget = 860000,  Spent = 860000, Beneficiaries = "4 200 p.a.",      ProgressPercent = 100, Status = "Complete" },
            new() { Name = "SMME incubator",           Category = "Enterprise development",Budget = 640000,  Spent = 410000, Beneficiaries = "18 businesses",   ProgressPercent = 64,  Status = "In progress" },
            new() { Name = "School feeding programme", Category = "Education",             Budget = 380000,  Spent = 285000, Beneficiaries = "1 860 learners",  ProgressPercent = 75,  Status = "Ongoing" },
            new() { Name = "Borehole rehabilitation",  Category = "Water & sanitation",    Budget = 520000,  Spent = 96000,  Beneficiaries = "1 400 households",ProgressPercent = 18,  Status = "In progress" }
        };

        public static List<AuditEntry> AuditLog => new()
        {
            new() { Timestamp = "22 Jul 2026 10:14:22", User = "P. Naidoo",  Role = "Safety Officer",   Action = "Update",  Entity = "Incident #1042",             OldValue = "Reported",         NewValue = "Acknowledged" },
            new() { Timestamp = "22 Jul 2026 09:41:07", User = "S. Dlamini", Role = "Employee",         Action = "Create",  Entity = "Incident #1042",             OldValue = "—",                NewValue = "Reported" },
            new() { Timestamp = "19 Jul 2026 14:37:02", User = "S. Nkosi",   Role = "Procurement",      Action = "Approve", Entity = "Supplier #482",              OldValue = "UnderReview",      NewValue = "Approved" },
            new() { Timestamp = "19 Jul 2026 14:36:44", User = "S. Nkosi",   Role = "Procurement",      Action = "Update",  Entity = "ComplianceDocument #1103",   OldValue = "Pending",          NewValue = "Verified" },
            new() { Timestamp = "19 Jul 2026 11:02:18", User = "S. Nkosi",   Role = "Procurement",      Action = "Update",  Entity = "Supplier #482",              OldValue = "Submitted",        NewValue = "UnderReview" },
            new() { Timestamp = "18 Jul 2026 16:20:55", User = "N. Sithole", Role = "HR & Operations",  Action = "Approve", Entity = "LeaveRequest #2214",         OldValue = "Pending",          NewValue = "Approved" },
            new() { Timestamp = "18 Jul 2026 16:20:55", User = "N. Sithole", Role = "HR & Operations",  Action = "Update",  Entity = "Employee #1044",             OldValue = "LeaveBalance 17.0", NewValue = "LeaveBalance 15.0" },
            new() { Timestamp = "18 Jul 2026 09:15:31", User = "T. Mokoena", Role = "Supplier",         Action = "Create",  Entity = "Supplier #482",              OldValue = "—",                NewValue = "Submitted" }
        };

        public static List<ApprovalItem> Approvals => new()
        {
            new() { Type = "Leave",     Requester = "S. Dlamini · EMP-1042",      Initials = "SD", AvatarColour = "#2C4A7A", Department = "HR & Operations",  Detail = "14–18 Aug 2026 · Annual · 5 days",        Waiting = "3 days",  Overdue = true },
            new() { Type = "Supplier",  Requester = "Bethal Logistics CC",        Initials = "BL", AvatarColour = "#1E6B3A", Department = "Procurement",      Detail = "Level 1 · 3 of 5 documents verified",     Waiting = "2 days",  Overdue = true },
            new() { Type = "CSR spend", Requester = "Bethal Skills Centre",       Initials = "BS", AvatarColour = "#8A5A2B", Department = "Community & CSR",  Detail = "R 1 200 000 · Q3 allocation",             Waiting = "2 days",  Overdue = true },
            new() { Type = "Leave",     Requester = "P. Naidoo · EMP-1043",       Initials = "PN", AvatarColour = "#4B79B5", Department = "HR & Operations",  Detail = "03–07 Sep 2026 · Study · 5 days",         Waiting = "1 day",   Overdue = false },
            new() { Type = "Supplier",  Requester = "Nkosi Plant Hire",           Initials = "NP", AvatarColour = "#6B3FA0", Department = "Procurement",      Detail = "Level 2 · all documents verified",        Waiting = "6 hours", Overdue = false }
        };

        /* Monthly Run of Mine tons as a percentage of the 9.7 Mt annual target, used by the dashboard chart. */
        public static int[] ProductionByMonth => new[] { 62, 71, 68, 80, 76, 88, 84, 92, 86, 95, 90, 97 };

        public static List<(string Unit, int Headcount, string Colour)> WorkforceByUnit => new()
        {
            ("Mining Operations", 118, "#1E3A63"),
            ("Processing & Logistics", 54, "#2C5691"),
            ("Corporate Services", 34, "#4B79B5"),
            ("Community & CSR", 22, "#22A55B"),
            ("Trading & Export", 20, "#D4A544")
        };
    }
}

/*
    Reference List:
        - Microsoft Learn. [s.a.]. Classes (C# Programming Guide). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/classes> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Collections (C#). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/collections> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Object and collection initializers (C# Programming Guide). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/object-and-collection-initializers> [Accessed 14 August 2026].
        - Microsoft Learn. [s.a.]. Static Classes and Static Class Members (C# Programming Guide). [online]. Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-classes-and-static-class-members> [Accessed 14 August 2026].
*/
