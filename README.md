# Overlooked Connect — Web

Two ASP.NET Core MVC applications sharing one design system and one REST API.

**Apex Solutions (Group 4) · INSY7315 · Client: The Overlooked Group**

| Project | What it is | Ports |
|---|---|---|
| `OverlookedConnect.PublicWeb` | Public corporate website, careers, supplier registration | 7083 / 5108 |
| `OverlookedConnect.InternalWeb` | Internal operations platform and staff app views | 7244 / 5275 |
| `OverlookedConnect.UI.Shared` | Shared Razor class library | — |

Both consume the shared API in `overlooked-connect-api` (7039 / 5089).

## Running

Start the API first — both sites degrade gracefully without it, but nothing is live.

```bash
# terminal 1 — the API
cd ../overlooked-connect-api
dotnet run --project src/OverlookedConnect.Api --launch-profile https

# terminal 2 — public site
dotnet run --project src/OverlookedConnect.PublicWeb --launch-profile https

# terminal 3 — internal platform
dotnet run --project src/OverlookedConnect.InternalWeb --launch-profile https
```

Development accounts all use the password `Overlooked#2026`.

| Email | Role | Lands on |
|---|---|---|
| t.mahlangu@overlookedgroup.co.za | Executive | Internal dashboard |
| n.sithole@overlookedgroup.co.za | HR | Internal dashboard |
| s.nkosi@overlookedgroup.co.za | Procurement | Internal dashboard |
| s.dlamini@overlookedgroup.co.za | Employee | Staff app views |
| thandiwe@bethallogistics.co.za | Supplier | Public status tracker |

## Staff single sign-on (OVC-267)

A staff member signs in on the **public** site and is carried into the internal platform without
entering credentials again.

```
public /StaffAccess/Login
    └─ POST /api/auth/login              → JWT issued by the API
        └─ Handoff.cshtml auto-POSTs the token (request body, not a URL)
            └─ internal /Account/Sso
                └─ GET /api/auth/me       → API confirms the token
                    └─ session established, redirect to the dashboard
```

Two decisions worth noting:

**The token travels in a POST body, never a query string.** A token in a URL is written to browser
history, server access logs and any outbound `Referer` header.

**The internal platform does not trust the token on arrival.** Anything in a request body is
attacker-controllable, so `/Account/Sso` presents it to the API's own `/api/auth/me` and only a 200
establishes a session. There is exactly one authority on whether a token is valid.

Suppliers signing in on the public site stay there and go to their application status tracker —
the back office is not their portal.

## What is connected to the API

| Screen | Endpoint | Ticket |
|---|---|---|
| Public — staff sign-in | `POST /api/auth/login` | OVC-267 |
| Public — supplier registration | `POST /api/suppliers` | OVC-247 |
| Public — application status | `GET /api/suppliers/mine` | OVC-248 |
| Public — careers listings | `GET /api/vacancies` | OVC-250 |
| Internal — sign-in | `POST /api/auth/login` | OVC-224 |
| Internal — SSO handover | `GET /api/auth/me` | OVC-267 |
| Internal — supplier queue | `GET /api/suppliers` | OVC-248 |
| Internal — review / approve / reject | `POST /api/suppliers/{id}/…` | OVC-249 |

The supplier queue shows a **LIVE API** or **DEMO DATA** pill so it is obvious which mode it is in.
Job applications and contact enquiries remain simulated; those endpoints belong to later tickets.

## Try the rule that matters

Sign in as `s.nkosi@overlookedgroup.co.za`, open **Suppliers**, and click **Approve** on Bethal
Logistics — 2 of 5 documents verified. The API refuses:

> Cannot approve: 3 of 5 compliance documents are not yet verified (…)

Approve Nkosi Plant Hire instead and it succeeds with a vendor number.

The rule lives on the `Supplier` entity in the API, not in this screen. If it lived here, the
Android client or a direct API call could bypass it.

## Configuration

Both sites read the API address from `appsettings.json`:

```json
"Api": { "BaseUrl": "https://localhost:7039/" }
```

The public site also holds `InternalWeb:BaseUrl` (the SSO target) and the internal site holds
`PublicWeb:BaseUrl` (the link back). Change these once when the apps move to Azure.

## Referencing

Every hand-written source file carries inline citations in the form `/* [Author, [s.a.]] */` and a
reference list block comment at the end, in IIE Harvard (Anglia) style.
