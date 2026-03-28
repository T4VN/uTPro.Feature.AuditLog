using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using uTPro.Feature.AuditLog.Models;
using uTPro.Feature.AuditLog.Services;

namespace uTPro.Feature.AuditLog.Controllers;

[VersionedApiBackOfficeRoute("utpro/audit-log")]
[ApiExplorerSettings(GroupName = "uTPro Audit Log")]
public class AuditLogApiController(IAuditLogService auditLogService) : ManagementApiControllerBase
{
    [HttpPost("audit-entries")]
    public IActionResult GetAuditEntries([FromBody] AuditLogFilterRequest filter)
        => Ok(auditLogService.GetAuditEntries(filter));

    [HttpPost("log-entries")]
    public IActionResult GetLogEntries([FromBody] AuditLogFilterRequest filter)
        => Ok(auditLogService.GetLogEntries(filter));

    [HttpPost("timeline")]
    public IActionResult GetTimeline([FromBody] AuditLogFilterRequest filter)
        => Ok(auditLogService.GetTimeline(filter));

    [HttpPost("event-types")]
    public IActionResult GetEventTypes() => Ok(auditLogService.GetDistinctEventTypes());

    [HttpPost("log-headers")]
    public IActionResult GetLogHeaders() => Ok(auditLogService.GetDistinctLogHeaders());

    [HttpPost("users")]
    public IActionResult GetUsers() => Ok(auditLogService.GetUsers());

    [HttpPost("export/audit-entries")]
    public IActionResult ExportAuditEntries([FromBody] AuditLogFilterRequest filter)
    {
        filter.Skip = 0;
        filter.Take = 50000;
        var data = auditLogService.GetAuditEntries(filter);
        var csv = CsvHelper.ToCsv(data.Items, new[] { "Date (UTC)", "User", "Event Type", "Details", "IP", "Affected" },
            i => new[] { i.EventDateUtc.ToString("o"), i.PerformingDetails, i.EventType, i.EventDetails, i.PerformingIp, i.AffectedDetails });
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "audit-entries.csv");
    }

    [HttpPost("export/log-entries")]
    public IActionResult ExportLogEntries([FromBody] AuditLogFilterRequest filter)
    {
        filter.Skip = 0;
        filter.Take = 50000;
        var data = auditLogService.GetLogEntries(filter);
        var csv = CsvHelper.ToCsv(data.Items, new[] { "Date", "User", "Log Type", "Comment", "Node ID", "Node Name", "Entity" },
            i => new[] { i.DateStamp.ToString("o"), i.UserName, i.LogHeader, i.LogComment, i.NodeId.ToString(), i.NodeName, i.EntityType });
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "log-entries.csv");
    }

    [HttpPost("export/timeline")]
    public IActionResult ExportTimeline([FromBody] AuditLogFilterRequest filter)
    {
        filter.Skip = 0;
        filter.Take = 50000;
        var data = auditLogService.GetTimeline(filter);
        var csv = CsvHelper.ToCsv(data.Items, new[] { "Date", "Source", "User", "Action", "Details", "Extra" },
            i => new[] { i.Date.ToString("o"), i.Source, i.User, i.Action, i.Details, i.Extra });
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "timeline.csv");
    }
}
