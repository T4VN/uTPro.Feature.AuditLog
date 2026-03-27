# uTPro.Feature.AuditLog

Audit Log viewer dashboard for **Umbraco 16+** backoffice. Browse `umbracoAudit` and `umbracoLog` tables with filtering, search, and pagination.

## Installation

```bash
dotnet add package uTPro.Feature.AuditLog
```

No configuration needed — auto-registers via Umbraco `IComposer`.

## Features

- Audit Trail tab (umbracoAudit)
- Content Logs tab (umbracoLog + user names)
- Filter by event type, search text, date range
- Server-side pagination

## Location

Settings section → Audit Log dashboard tab.

## Compatibility

| Umbraco | .NET | Package |
|---------|------|---------|
| 16.x+   | 9.0  | 1.0.x   |

## License

MIT
