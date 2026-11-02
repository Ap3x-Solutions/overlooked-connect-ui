# Overlooked Connect — Web Applications

**Apex Solutions | Group 4 | INSY7315**

**Client:** The Overlooked Group

Overlooked Connect is an ASP.NET Core MVC solution consisting of public-facing and internal web applications, supported by a shared REST API. The platform provides interfaces for recruitment, supplier management, employee self-service and internal operations.

## 1. Solution Architecture

| Project | Responsibility |
|---|---|
| `OverlookedConnect.PublicWeb` | Public website, careers, supplier registration and public-facing workflows |
| `OverlookedConnect.InternalWeb` | Staff portal, internal operations, reports, audit and administrative workflows |
| `OverlookedConnect.UI.Shared` | Shared UI components and presentation resources |
| `OverlookedConnect.PublicWeb.Tests` | Public web application tests |
| `OverlookedConnect.InternalWeb.Tests` | Internal web application tests |

Both web applications communicate with the separate `overlooked-connect-api` repository.

**Backend repository:** https://github.com/Ap3x-Solutions/overlooked-connect-api

## 2. Technology Stack

- ASP.NET Core MVC and Razor views
- C# and .NET
- HTML, CSS and JavaScript
- REST API integration
- JWT-based authentication through the backend API
- Git and GitHub
- GitHub Actions for automated build and test validation
- Microsoft Azure for web application hosting

## 3. Application Features

### Public Web Application

The public application provides the corporate-facing interface and entry points for external users.

Key areas include:

- Public website and company information
- Careers and vacancy listings
- Supplier registration
- Supplier application status
- Staff authentication and handoff to the internal platform

### Internal Web Application

The internal application provides role-based access to operational workflows.

Key areas include:

- Internal dashboard
- Employee management
- Supplier review and approval
- Reports and reporting views
- Audit activity and audit-related views
- Staff self-service interfaces

Some interfaces may contain demonstration content or incomplete integrations. A visible page does not necessarily mean its underlying API workflow is complete.

### Reports and Audit

Recent development changes include:

- Alignment of Reports authorization with backend roles
- Access to Reports for authorized Executive, HR and Safety roles
- Separate Reports and Audit navigation entries
- Updates to reporting and audit-related controllers, views and API client integration

Authorization must be enforced by the server and backend API, not only by navigation visibility.

## 4. Authentication and Authorization

Overlooked Connect uses the backend API to authenticate users.

The staff sign-in flow supports a handoff from the public website to the internal platform:

1. A staff member signs in through the public website.
2. The backend API authenticates the user and issues a JWT.
3. The public website submits the token to the internal application's SSO endpoint using an HTTP POST.
4. The internal application validates the token with the backend API.
5. An internal session is established for the authenticated user.

The token is not passed through a URL query string.

Access to internal features depends on the authenticated user's role and the authorization rules of the relevant controller and API endpoint.

## 5. Getting Started

### Prerequisites

- .NET SDK compatible with the solution projects
- Git
- Visual Studio or Visual Studio Code
- Access to the Overlooked Connect backend API
- Appropriate local configuration

### Clone the UI repository

```powershell
git clone https://github.com/Ap3x-Solutions/overlooked-connect-ui.git
cd overlooked-connect-ui
```

### Restore and build

```powershell
dotnet restore OverlookedConnect.UI.sln
dotnet build OverlookedConnect.UI.sln
```

### Run the backend API

From the backend repository:

```powershell
dotnet run --project src/OverlookedConnect.Api --launch-profile https
```

### Run the public website

From the UI repository:

```powershell
dotnet run --project src/OverlookedConnect.PublicWeb --launch-profile https
```

### Run the internal website

In another terminal:

```powershell
dotnet run --project src/OverlookedConnect.InternalWeb --launch-profile https
```

The backend API must be available for workflows that require live data.

## 6. Configuration

Both web applications use an `Api:BaseUrl` configuration setting to identify the backend API.

Example local configuration:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7039/"
  }
}
```

The public application also uses `InternalWeb:BaseUrl` for internal-platform navigation and SSO handoff. The internal application uses `PublicWeb:BaseUrl` for links back to the public website.

Do not commit passwords, JWTs, API keys, production connection strings or other secrets.

Use approved environment configuration and secret-management mechanisms for sensitive settings.

## 7. Testing and Continuous Integration

The solution contains separate test projects for the public and internal web applications.

Run the complete test suite using:

```powershell
dotnet test OverlookedConnect.UI.sln
```

GitHub Actions is used to validate UI builds and tests.

For the November 2026 release preparation, PR #38 passed UI CI, and release PR #39 passed both its push and pull-request checks.

Successful CI checks verify the configured automated checks; they do not replace manual end-to-end testing of deployed workflows.

## 8. Deployment

The solution supports deployment of the public and internal web applications to Microsoft Azure.

Deployment configuration should provide:

- The correct backend API base URL
- Public and internal website URLs
- HTTPS configuration
- Required environment settings
- Appropriate access controls

Verify the live application after deployment, including authentication, role-based navigation, API connectivity and important user workflows.

## 9. Known Limitations

- Payslip viewing is represented in the staff interface, but a functional payroll data source and PDF download workflow have not been verified.
- External payroll-provider integration is outside the original project scope.
- Features with demonstration data must not be presented as live production integrations.
- Android functionality is maintained separately and is not validated by the UI repository's CI checks.

## 10. Development Workflow

The repository uses a branch-based development process:

- `develop` — integration of reviewed development changes
- `main` — release branch
- Feature and fix branches — isolated development work

Changes should be reviewed through pull requests, with build and test checks completed before merging.

## 11. Documentation

Additional project documentation is maintained in the `docs/` directory, including accessibility and source-reference materials.

Source-code references follow the academic referencing conventions used by the project team.

## 12. Project Team

Developed by **Apex Solutions — Group 4**, as part of the INSY7315 academic project for The Overlooked Group.

---

**Project:** Overlooked Connect  
**Repository:** https://github.com/Ap3x-Solutions/overlooked-connect-ui  
**Release documentation update:** November 2026
