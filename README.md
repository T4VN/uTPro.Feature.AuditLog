# <img width="50" height="50" alt="icon" src="https://github.com/user-attachments/assets/a11d9f9f-fb3e-445c-86a4-401cc8aee392" /> uTPro.Feature.AuditLog

A backoffice dashboard for **Umbraco 16+** that gives you full visibility into your CMS activity — audit trails, content logs, and a unified timeline view — all in one place.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.AuditLog.svg)](https://www.nuget.org/packages/uTPro.Feature.AuditLog)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.auditlog)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Why?

Umbraco stores audit and log data in `umbracoAudit` and `umbracoLog` tables, but there's no built-in UI to browse them. When you need to track who did what and when — debugging content changes, investigating user actions, or compliance auditing — you're left writing SQL queries.

**uTPro.Feature.AuditLog** solves this by providing a clean, searchable dashboard right in the backoffice.

## Features

### 3 Views
- **Timeline** — Merged view of both audit and log data, sorted chronologically. Track a user's complete activity flow without switching tabs.
- **Content Logs** — Browse `umbracoLog` entries with user names, node info, and entity types.
- **Audit Trail** — Browse `umbracoAudit` entries with performing user, affected user, IP addresses, and event details.

### Filtering & Search
- Full-text search across all relevant columns (details, user names, IPs, event types, comments, node IDs)
- Filter by **performing user** (all tabs)
- Filter by **affected user** (audit trail)
- Filter by **event type / log header**
- Filter by **date range**
- Search on Enter key

### Export
- **Export CSV** — Download full dataset (up to 50k records) with current filters applied. Available on all tabs.

### Display
- **UTC / Local time toggle** — Switch between local time and UTC display
- **Consistent user format** — `Admin <admin@example.com>` across all views
- **Server-side pagination** — Handles large datasets efficiently
- **Fixed column widths** — Date and User columns stay consistent across tabs

## Installation

```bash
dotnet add package uTPro.Feature.AuditLog
```

That's it. No configuration needed — the package auto-registers via Umbraco `IComposer` and appears as a dashboard in the Settings section.

## Compatibility

| Umbraco | .NET | Package |
|---------|------|---------|
| 16.x    | 9.0  | 1.x     |

## Development

### Prerequisites
- .NET 9 SDK
- An Umbraco 16 site (or use the included TestSite)

### Run the TestSite

```bash
dotnet run --project src/uTPro.Feature.AuditLog.TestSite
```

Navigate to `https://localhost:54730/umbraco` and log in with:
- Email: `admin@example.com`
- Password: `Admin1234!`

### Build NuGet Package

```bash
dotnet build src/uTPro.Feature.AuditLog -c Release
```

The `.nupkg` file is output to the `Build/` folder automatically.

## Author

**T4VN** — [GitHub](https://github.com/T4VN)

## License

[MIT](LICENSE)
