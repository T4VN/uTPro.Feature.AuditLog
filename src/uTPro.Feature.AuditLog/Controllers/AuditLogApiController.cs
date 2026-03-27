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

    [HttpPost("event-types")]
    public IActionResult GetEventTypes() => Ok(auditLogService.GetDistinctEventTypes());

    [HttpPost("log-headers")]
    public IActionResult GetLogHeaders() => Ok(auditLogService.GetDistinctLogHeaders());
}
