using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

internal class AuditLogComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IAuditLogService, AuditLogService>();
}

public interface IAuditLogService
{
    AuditLogPagedResult<AuditEntryViewModel> GetAuditEntries(AuditLogFilterRequest filter);
    AuditLogPagedResult<LogEntryViewModel> GetLogEntries(AuditLogFilterRequest filter);
    IEnumerable<string> GetDistinctEventTypes();
    IEnumerable<string> GetDistinctLogHeaders();
}

internal class AuditLogService(IScopeProvider scopeProvider, ILogger<AuditLogService> logger) : IAuditLogService
{
    public readonly string tableUmbracoLog = "umbracoLog";
    public readonly string tableUmbracoAudit = "umbracoAudit";
    public AuditLogPagedResult<AuditEntryViewModel> GetAuditEntries(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql().Select("*").From(tableUmbracoAudit);
        var where = new List<string>(); var parms = new List<object>(); int p = 0;

        if (!string.IsNullOrWhiteSpace(filter.EventType))
        { where.Add($"eventType = @{p}"); parms.Add(filter.EventType); p++; }
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        { where.Add($"(eventDetails LIKE @{p} OR performingDetails LIKE @{p} OR affectedDetails LIKE @{p})"); parms.Add($"%{filter.SearchTerm}%"); p++; }
        if (filter.DateFrom.HasValue) { where.Add($"eventDateUtc >= @{p}"); parms.Add(filter.DateFrom.Value); p++; }
        if (filter.DateTo.HasValue) { where.Add($"eventDateUtc <= @{p}"); parms.Add(filter.DateTo.Value); p++; }

        if (where.Count > 0) sql = sql.Where(string.Join(" AND ", where), parms.ToArray());
        sql = sql.OrderByDescending("eventDateUtc");

        try
        {
            var page = scope.Database.Page<AuditEntryDto>(filter.Skip / Math.Max(filter.Take, 1) + 1, filter.Take, sql);
            return new AuditLogPagedResult<AuditEntryViewModel>
            {
                Items = page.Items.Select(d => new AuditEntryViewModel
                {
                    Id = d.Id, EventDateUtc = d.EventDateUtc, PerformingUserId = d.PerformingUserId,
                    PerformingDetails = d.PerformingDetails ?? "", PerformingIp = d.PerformingIp ?? "",
                    EventType = d.EventType ?? "", EventDetails = d.EventDetails ?? "",
                    AffectedUserId = d.AffectedUserId, AffectedDetails = d.AffectedDetails ?? ""
                }),
                Total = page.TotalItems
            };
        }
        catch (Exception ex) { logger.LogError(ex, "Error fetching audit entries"); return new() { Items = [], Total = 0 }; }
    }

    public AuditLogPagedResult<LogEntryViewModel> GetLogEntries(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("l.id, l.userId, l.DateStamp, l.logHeader, l.logComment, l.NodeId, l.entityType, u.userName")
            .From($"{tableUmbracoLog} l").LeftJoin("umbracoUser u").On("l.userId = u.id");
        var where = new List<string>(); var parms = new List<object>(); int p = 0;

        if (!string.IsNullOrWhiteSpace(filter.EventType))
        { where.Add($"l.logHeader = @{p}"); parms.Add(filter.EventType); p++; }
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        { where.Add($"(l.logComment LIKE @{p} OR u.userName LIKE @{p})"); parms.Add($"%{filter.SearchTerm}%"); p++; }
        if (filter.DateFrom.HasValue) { where.Add($"l.DateStamp >= @{p}"); parms.Add(filter.DateFrom.Value); p++; }
        if (filter.DateTo.HasValue) { where.Add($"l.DateStamp <= @{p}"); parms.Add(filter.DateTo.Value); p++; }

        if (where.Count > 0) sql = sql.Where(string.Join(" AND ", where), parms.ToArray());
        sql = sql.OrderByDescending("l.DateStamp");

        try
        {
            var page = scope.Database.Page<LogEntryDto>(filter.Skip / Math.Max(filter.Take, 1) + 1, filter.Take, sql);
            return new AuditLogPagedResult<LogEntryViewModel>
            {
                Items = page.Items.Select(d => new LogEntryViewModel
                {
                    Id = d.Id, UserId = d.UserId, UserName = d.UserName ?? $"User {d.UserId}",
                    DateStamp = d.DateStamp, LogHeader = d.LogHeader ?? "", LogComment = d.LogComment ?? "",
                    NodeId = d.NodeId, EntityType = d.EntityType ?? ""
                }),
                Total = page.TotalItems
            };
        }
        catch (Exception ex) { logger.LogError(ex, "Error fetching log entries"); return new() { Items = [], Total = 0 }; }
    }

    public IEnumerable<string> GetDistinctEventTypes()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        try { return scope.Database.Fetch<string>(scope.SqlContext.Sql().Select("DISTINCT eventType").From(tableUmbracoAudit)); }
        catch (Exception ex) { logger.LogError(ex, "Error fetching event types"); return []; }
    }

    public IEnumerable<string> GetDistinctLogHeaders()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        try { return scope.Database.Fetch<string>(scope.SqlContext.Sql().Select("DISTINCT logHeader").From(tableUmbracoLog)); }
        catch (Exception ex) { logger.LogError(ex, "Error fetching log headers"); return []; }
    }
}
