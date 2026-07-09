using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

public interface IAuditLogService
{
    AuditLogPagedResult<AuditEntryViewModel> GetAuditEntries(AuditLogFilterRequest filter);
    AuditLogPagedResult<LogEntryViewModel> GetLogEntries(AuditLogFilterRequest filter);
    AuditLogPagedResult<TimelineEntryViewModel> GetTimeline(AuditLogFilterRequest filter);
    IEnumerable<string> GetDistinctEventTypes();
    IEnumerable<string> GetDistinctLogHeaders();
    IEnumerable<UserInfoViewModel> GetUsers();
}
