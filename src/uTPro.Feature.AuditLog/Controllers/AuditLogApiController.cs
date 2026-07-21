using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Authorization;
using uTPro.Feature.AuditLog.Models;
using uTPro.Feature.AuditLog.Services;

namespace uTPro.Feature.AuditLog.Controllers;

// Authorization decision:
// Audit logs expose every user's activity, including emails and IP addresses, so access is
// restricted to backoffice administrators. We keep the [Authorize] SectionAccessSettings
// policy (so a caller must have Settings-section access at all) AND add an explicit
// administrators-group membership check via IsAdmin(): every action returns Forbid() when the
// current user is not in the built-in administrators group. Umbraco does not ship a dedicated
// "admin-only" authorization policy for management API controllers, so the group check is the
// most direct and reliable admin gate.
[VersionedApiBackOfficeRoute("utpro/audit-log")]
[MapToApi(ConfigureAuditLogSwaggerGenOptions.ApiName)]
[ApiExplorerSettings(GroupName = "Audit Log")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AuditLogApiController(
    IAuditLogService auditLogService,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor) : ManagementApiControllerBase
{
    /// <summary>
    /// True when the current backoffice user belongs to the built-in administrators group.
    /// </summary>
    private bool IsAdmin()
    {
        var currentUser = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        return currentUser is not null
            && currentUser.Groups.Any(g => g.Alias == Constants.Security.AdminGroupAlias);
    }

    /// <summary>
    /// Returns a Forbid() result when the current user is not an administrator, otherwise null.
    /// Call at the start of every action to gate access to the (sensitive) audit data.
    /// </summary>
    private IActionResult? EnsureAdmin()
        => IsAdmin() ? null : Forbid();

    [HttpPost("audit-entries")]
    public IActionResult GetAuditEntries([FromBody] AuditLogFilterRequest filter)
        => EnsureAdmin() ?? Ok(auditLogService.GetAuditEntries(filter));

    [HttpPost("log-entries")]
    public IActionResult GetLogEntries([FromBody] AuditLogFilterRequest filter)
        => EnsureAdmin() ?? Ok(auditLogService.GetLogEntries(filter));

    [HttpPost("timeline")]
    public IActionResult GetTimeline([FromBody] AuditLogFilterRequest filter)
        => EnsureAdmin() ?? Ok(auditLogService.GetTimeline(filter));

    [HttpPost("event-types")]
    public IActionResult GetEventTypes()
        => EnsureAdmin() ?? Ok(auditLogService.GetDistinctEventTypes());

    [HttpPost("log-headers")]
    public IActionResult GetLogHeaders()
        => EnsureAdmin() ?? Ok(auditLogService.GetDistinctLogHeaders());

    [HttpPost("users")]
    public IActionResult GetUsers()
        => EnsureAdmin() ?? Ok(auditLogService.GetUsers());

    [HttpPost("export/audit-entries")]
    public IActionResult ExportAuditEntries([FromBody] AuditLogFilterRequest filter)
    {
        if (EnsureAdmin() is { } forbid) return forbid;

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
        if (EnsureAdmin() is { } forbid) return forbid;

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
        if (EnsureAdmin() is { } forbid) return forbid;

        filter.Skip = 0;
        filter.Take = 50000;
        var data = auditLogService.GetTimeline(filter);
        var csv = CsvHelper.ToCsv(data.Items, new[] { "Date", "Source", "User", "Action", "Details", "Extra" },
            i => new[] { i.Date.ToString("o"), i.Source, i.User, i.Action, i.Details, i.Extra });
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "timeline.csv");
    }
}
