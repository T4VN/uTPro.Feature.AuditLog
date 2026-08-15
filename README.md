# uTPro Audit Log Viewer for Umbraco

The missing audit log & content log viewer for the Umbraco backoffice. Browse, search, filter, sort, and export every login, save, publish, move, and delete in your Umbraco site — straight from the Settings section, with no SQL queries.

Supports **Umbraco 16, 17 and 18**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.AuditLog.svg)](https://www.nuget.org/packages/uTPro.Feature.AuditLog)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.AuditLog.svg)](https://www.nuget.org/packages/uTPro.Feature.AuditLog)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.auditlog)
[![Umbraco 16+](https://img.shields.io/badge/Umbraco-16%2B-3544B1)](https://umbraco.com)
[![License: Free (proprietary)](https://img.shields.io/badge/License-Free%20(proprietary)-green.svg)](LICENSE.txt)

![uTPro Audit Log Viewer](https://raw.githubusercontent.com/T4VN/uTPro.Feature.AuditLog/refs/heads/main/Image/Screenshots-v3.0.0/Screenshot-Timeline.png)

---

## Features

- **Three views** — Timeline, Content Logs, Audit Trail
- Full-text search across all fields
- Rich filtering by user, event type, date range
- Quick date ranges (This month, Last 30/7 days, Today, Custom)
- Sortable columns (server-side)
- Quick edit links to content editors
- Local / UTC time toggle
- Export to CSV (up to 50,000 rows)
- Shareable & bookmarkable filters (saved in URL)
- Admin-only access, read-only (no schema changes)
- Works on SQL Server, SQLite and PostgreSQL

---

## Quick Start

```bash
dotnet add package uTPro.Feature.AuditLog
```

Navigate to **Settings → Advanced → Audit Log Viewer**. No configuration required.

| Umbraco | .NET | Target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

---

## 📖 Full Documentation

**[docs.utpro.dev/uTPro.Feature.AuditLog](https://docs.utpro.dev/uTPro.Feature.AuditLog/)**

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](LICENSE.txt).

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.AuditLog) · [GitHub](https://github.com/T4VN/uTPro.Feature.AuditLog) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.auditlog)
