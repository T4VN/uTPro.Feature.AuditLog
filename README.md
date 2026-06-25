# <img width="42" height="42" alt="icon" src="https://github.com/user-attachments/assets/a11d9f9f-fb3e-445c-86a4-401cc8aee392" align="absmiddle" /> uTPro Audit Log Viewer

> Read, search, and export your Umbraco activity — audit trails, content logs, and a unified timeline — straight from the backoffice. No SQL required. Supports **Umbraco 16, 17 and 18**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.AuditLog.svg)](https://www.nuget.org/packages/uTPro.Feature.AuditLog)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.auditlog)
[![Umbraco 16 · 17 · 18](https://img.shields.io/badge/Umbraco-16%20%C2%B7%2017%20%C2%B7%2018-3544B1)](https://umbraco.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

<img src="https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots/Screenshot-1.png" alt="Timeline view" width="100%" />

---

## Contents

- [What it does](#what-it-does)
- [Where to find it](#where-to-find-it)
- [The three views](#the-three-views)
- [Features](#features)
- [Installation](#installation)
- [Compatibility](#compatibility)
- [How it works](#how-it-works)
- [Development](#development)
- [FAQ](#faq)
- [License](#license)

---

## What it does

Every login, save, publish, move, and delete in Umbraco is recorded in the `umbracoAudit` and `umbracoLog` database tables. The catch: there is **no built-in screen** to read them. Answering a simple question like *"who unpublished this page last week?"* usually means writing a SQL query against the database.

**uTPro Audit Log Viewer** adds that missing screen. It gives you a fast, searchable, filterable view of all that activity — and lets you export it to CSV — without touching the database.

---

## Where to find it

Once installed, open:

> **Settings → Advanced → Audit Log Viewer**

It sits in the left sidebar, right below the built-in *Log Viewer*.

Access is limited to users who can see the **Settings** section, so audit data stays in front of the right people only.

---

## The three views

The screen is split into three tabs along the top.

### 1. Timeline
Audit and log entries **merged into a single chronological list**. This is the quickest way to follow what happened across the site over time, regardless of where Umbraco stored it.

<img src="https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots/Screenshot-1.png" alt="Timeline" width="100%" />

### 2. Content Logs
Entries from `umbracoLog` — content and media actions such as Save, Publish, Move, and Delete — with the user, node ID, node name, and entity type.

<img src="https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots/Screenshot-2.png" alt="Content Logs" width="100%" />

### 3. Audit Trail
Entries from `umbracoAudit` — user-level events such as sign-in, password reset, and profile changes — with the performing user, affected user, IP address, and event details.

<img src="https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots/Screenshot-3.png" alt="Audit Trail" width="100%" />

| Tab | Source table | Best for |
|-----|--------------|----------|
| **Timeline** | `umbracoAudit` + `umbracoLog` | Seeing the full activity flow in order |
| **Content Logs** | `umbracoLog` | Tracking content & media changes |
| **Audit Trail** | `umbracoAudit` | Investigating user/security events |

---

## Features

### 🔎 Search & filter
- Full-text search across the relevant columns of each tab (details, user, IP, event type, comment, node ID)
- Filter by **performing user** — on every tab
- Filter by **affected user** — on the Audit Trail
- Filter by **event type / log header**
- Filter by **date range** (from / to)
- Hit **Enter** in the search box to apply right away; **Reset** clears everything

<img src="https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots/Screenshot-4.png" alt="Filtering" width="100%" />

### 🕐 Local or UTC time
- One click switches the whole table between **your local time** and **UTC+0**
- The local button shows your real offset, e.g. `GMT+7`, so there is no guessing
- Audit data is stored in UTC and log data in server-local time — the Timeline reconciles the two so the ordering is always correct (on both SQL Server and SQLite)

<img src="https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots/Screenshot-5.png" alt="UTC time" width="100%" />

### 📄 Pagination
- Fast **server-side paging** for large tables
- **Jump to page** — type a page number and press Enter to go straight there

### ⬇️ Export to CSV
- Export the full result set (up to **50,000 rows**) with your current filters applied
- Works on any of the three tabs

<img src="https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots/Screenshot-6-exportCSV.png" alt="Export CSV" width="100%" />

### 🔒 Secure by default
- All API endpoints require **Settings section** access — no extra setup needed

---

## Installation

```bash
dotnet add package uTPro.Feature.AuditLog
```

Then run your site. That's it — the package registers itself automatically and the **Audit Log Viewer** item appears under *Settings → Advanced*. There is nothing to configure.

---

## Compatibility

| Umbraco | .NET | Database |
|---------|------|----------|
| 16.x | 9.0 | SQL Server · SQLite |
| 17.x | 10.0 | SQL Server · SQLite |
| 18.x | 10.0 | SQL Server · SQLite |

The package multi-targets `net9.0` (Umbraco 16) and `net10.0` (Umbraco 17 & 18), so a single install picks the right build for your project automatically.

---

## How it works

The package is a self-contained Razor Class Library:

- A **Management API controller** (`/umbraco/management/api/v1/utpro/audit-log/...`) reads the data, guarded by the Settings-section authorization policy.
- A **scoped service** queries `umbracoAudit` and `umbracoLog` using Umbraco's scope provider, with parameterized SQL and joins to `umbracoUser` and `umbracoNode` for friendly names. It auto-detects SQL Server vs SQLite for the timeline's date math.
- A **Lit-based backoffice extension** registers a menu item plus three workspace-view tabs (Timeline, Content Logs, Audit Trail).

No new database tables are created and no existing data is modified — the package is **read-only** apart from generating CSV exports.

---

## Development

### Prerequisites
- .NET 9 SDK **and** .NET 10 SDK (the package multi-targets both)
- An Umbraco 16, 17, or 18 site (or use the included TestSite, which runs on Umbraco 18)

### Run the TestSite

```bash
dotnet run --project src/uTPro.Feature.AuditLog.TestSite
```

Open `https://localhost:54730/umbraco` and sign in:

- **Email:** `admin@example.com`
- **Password:** `Admin1234!`

### Build the NuGet package

```bash
dotnet build src/uTPro.Feature.AuditLog -c Release
```

The `.nupkg` is written to the `Build/` folder automatically.

---

## FAQ

**Does it change or delete any of my data?**
No. It only reads from `umbracoAudit` and `umbracoLog`. The only output it produces is the CSV file you choose to download.

**Who can see the audit data?**
Only backoffice users with access to the **Settings** section.

**Why are some users shown as `SYSTEM` or `UNKNOWN`?**
Those entries were written by Umbraco itself (background tasks) or by a user account that no longer resolves — the viewer surfaces them exactly as recorded.

**Can I export more than 50,000 rows?**
The export is capped at 50,000 rows per file to keep it responsive. Narrow the result with filters (date range, user, search) and export in batches if you need more.

---

## Author

**T4VN** — [GitHub](https://github.com/T4VN) · [t4vn.com](https://t4vn.com)

## License

Released under the [MIT](LICENSE) license.
