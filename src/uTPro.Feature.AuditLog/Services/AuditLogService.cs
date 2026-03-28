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
    AuditLogPagedResult<TimelineEntryViewModel> GetTimeline(AuditLogFilterRequest filter);
    IEnumerable<string> GetDistinctEventTypes();
    IEnumerable<string> GetDistinctLogHeaders();
    IEnumerable<UserInfoViewModel> GetUsers();
}

internal class AuditLogService(IScopeProvider scopeProvider, ILogger<AuditLogService> logger) : IAuditLogService
{
    private const string TableAudit = "umbracoAudit";
    private const string TableLog = "umbracoLog";
    private const string TableUser = "umbracoUser";
    private const string TableNode = "umbracoNode";

    // Negate offset: to convert local time → UTC, subtract the offset
    private static readonly string NegatedUtcOffsetHours =
        (-TimeZoneInfo.Local.BaseUtcOffset.TotalHours).ToString("+0;-0");

    #region Audit Entries

    public AuditLogPagedResult<AuditEntryViewModel> GetAuditEntries(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var sql = scope.SqlContext.Sql()
            .Select(@"au.*, 
                      p.userName AS performingUserName, p.userEmail AS performingEmail,
                      a.userName AS affectedUserName, a.userEmail AS affectedEmail")
            .From($"{TableAudit} au")
            .LeftJoin($"{TableUser} p").On("au.performingUserId = p.id")
            .LeftJoin($"{TableUser} a").On("au.affectedUserId = a.id");

        var conditions = new List<string>();
        var parameters = new List<object>();
        var paramIndex = 0;

        AddCondition(conditions, parameters, ref paramIndex,
            filter.EventType, $"au.eventType = @{paramIndex}");

        AddCondition(conditions, parameters, ref paramIndex,
            filter.UserId, $"au.performingUserId = @{paramIndex}");

        AddCondition(conditions, parameters, ref paramIndex,
            filter.AffectedUserId, $"au.affectedUserId = @{paramIndex}");

        AddSearchCondition(conditions, parameters, ref paramIndex,
            filter.SearchTerm,
            "au.eventDetails", "au.performingDetails", "au.affectedDetails",
            "au.performingIp", "au.eventType", "p.userName", "a.userName");

        AddDateRange(conditions, parameters, ref paramIndex,
            filter.DateFrom, filter.DateTo, "au.eventDateUtc");

        if (conditions.Count > 0)
            sql = sql.Where(string.Join(" AND ", conditions), parameters.ToArray());

        sql = sql.OrderByDescending("au.eventDateUtc").OrderByDescending("au.id");

        try
        {
            var pageNumber = CalculatePageNumber(filter.Skip, filter.Take);
            var page = scope.Database.Page<AuditEntryDto>(pageNumber, filter.Take, sql);

            return new AuditLogPagedResult<AuditEntryViewModel>
            {
                Items = page.Items.Select(MapToAuditEntryViewModel),
                Total = page.TotalItems
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching audit entries");
            return new() { Items = [], Total = 0 };
        }
    }

    private static AuditEntryViewModel MapToAuditEntryViewModel(AuditEntryDto dto) => new()
    {
        Id = dto.Id,
        EventDateUtc = DateTime.SpecifyKind(dto.EventDateUtc, DateTimeKind.Utc),
        PerformingUserId = dto.PerformingUserId,
        PerformingDetails = FormatUserDisplay(dto.PerformingUserName, dto.PerformingEmail)
            ?? dto.PerformingDetails ?? "",
        PerformingIp = dto.PerformingIp ?? "",
        EventType = dto.EventType ?? "",
        EventDetails = dto.EventDetails ?? "",
        AffectedUserId = dto.AffectedUserId,
        AffectedDetails = dto.AffectedDetails ?? ""
    };

    #endregion

    #region Log Entries

    public AuditLogPagedResult<LogEntryViewModel> GetLogEntries(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var sql = scope.SqlContext.Sql()
            .Select(@"l.id, l.userId, l.DateStamp, l.logHeader, l.logComment, l.NodeId, l.entityType,
                      CASE WHEN u.userName IS NULL THEN 'SYSTEM' ELSE u.userName END AS Username,
                      u.userEmail, n.[Text]")
            .From($"{TableLog} l")
            .LeftJoin($"{TableUser} u").On("l.userId = u.id")
            .LeftJoin($"{TableNode} n").On("n.id = l.nodeId");

        var conditions = new List<string>();
        var parameters = new List<object>();
        var paramIndex = 0;

        AddCondition(conditions, parameters, ref paramIndex,
            filter.EventType, $"l.logHeader = @{paramIndex}");

        AddCondition(conditions, parameters, ref paramIndex,
            filter.UserId, $"l.userId = @{paramIndex}");

        AddSearchCondition(conditions, parameters, ref paramIndex,
            filter.SearchTerm,
            "l.logComment", "u.userName", "l.entityType", "l.logHeader", "CAST(l.NodeId AS VARCHAR)");

        AddDateRange(conditions, parameters, ref paramIndex,
            filter.DateFrom, filter.DateTo, "l.DateStamp");

        if (conditions.Count > 0)
            sql = sql.Where(string.Join(" AND ", conditions), parameters.ToArray());

        sql = sql.OrderByDescending("l.DateStamp").OrderByDescending("l.id");

        try
        {
            var pageNumber = CalculatePageNumber(filter.Skip, filter.Take);
            var page = scope.Database.Page<LogEntryDto>(pageNumber, filter.Take, sql);

            return new AuditLogPagedResult<LogEntryViewModel>
            {
                Items = page.Items.Select(MapToLogEntryViewModel),
                Total = page.TotalItems
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching log entries");
            return new() { Items = [], Total = 0 };
        }
    }

    private static LogEntryViewModel MapToLogEntryViewModel(LogEntryDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        UserName = FormatUserDisplay(dto.UserName, dto.UserEmail)
            ?? $"User {dto.UserId}",
        DateStamp = dto.DateStamp,
        LogHeader = dto.LogHeader ?? "",
        LogComment = dto.LogComment ?? "",
        NodeId = dto.NodeId,
        EntityType = dto.EntityType ?? "",
        NodeName = dto.Text ?? ""
    };

    #endregion

    #region Timeline

    public AuditLogPagedResult<TimelineEntryViewModel> GetTimeline(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        try
        {
            var parameters = new List<object>();
            var paramIndex = 0;
            var auditConditions = new List<string>();
            var logConditions = new List<string>();

            if (filter.UserId.HasValue)
            {
                auditConditions.Add($"a.performingUserId = @{paramIndex}");
                logConditions.Add($"l.userId = @{paramIndex}");
                parameters.Add(filter.UserId.Value);
                paramIndex++;
            }

            if (filter.AffectedUserId.HasValue)
            {
                auditConditions.Add($"a.affectedUserId = @{paramIndex}");
                parameters.Add(filter.AffectedUserId.Value);
                paramIndex++;
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                auditConditions.Add($"(a.eventDetails LIKE @{paramIndex} OR a.performingDetails LIKE @{paramIndex} OR a.eventType LIKE @{paramIndex})");
                logConditions.Add($"(l.logComment LIKE @{paramIndex} OR l.logHeader LIKE @{paramIndex})");
                parameters.Add($"%{filter.SearchTerm}%");
                paramIndex++;
            }

            if (filter.DateFrom.HasValue)
            {
                auditConditions.Add($"a.eventDateUtc >= @{paramIndex}");
                logConditions.Add($"l.DateStamp >= @{paramIndex}");
                parameters.Add(filter.DateFrom.Value);
                paramIndex++;
            }

            if (filter.DateTo.HasValue)
            {
                auditConditions.Add($"a.eventDateUtc <= @{paramIndex}");
                logConditions.Add($"l.DateStamp <= @{paramIndex}");
                parameters.Add(filter.DateTo.Value);
                paramIndex++;
            }

            var auditWhereClause = auditConditions.Count > 0
                ? " WHERE " + string.Join(" AND ", auditConditions) : "";
            var logWhereClause = logConditions.Count > 0
                ? " WHERE " + string.Join(" AND ", logConditions) : "";

            var unionSql = $@"
                SELECT * FROM (
                    SELECT a.eventDateUtc AS Date, 'audit' AS Source, a.performingUserId AS UserId,
                           CASE WHEN pu.userName IS NULL THEN a.performingDetails ELSE pu.userName END AS [User],
                           pu.userEmail AS UserEmail,
                           a.eventType AS Action,
                           a.affectedDetails AS Details,
                           a.eventDetails AS Extra,
                           0 AS NodeId, NULL AS NodeName,
                           a.eventDateUtc AS SortDate
                    FROM {TableAudit} a
                    LEFT JOIN {TableUser} pu ON a.performingUserId = pu.id{auditWhereClause}
                    UNION ALL
                    SELECT l.DateStamp AS Date, 'log' AS Source, l.userId AS UserId,
                           CASE WHEN u.userName IS NULL THEN 'SYSTEM' ELSE u.userName END AS [User],
                           u.userEmail AS UserEmail,
                           l.logHeader AS Action, l.logComment AS Details, l.entityType AS Extra,
                           l.NodeId AS NodeId, n.[Text] AS NodeName,
                           datetime(l.DateStamp, '{NegatedUtcOffsetHours} hours') AS SortDate
                    FROM {TableLog} l
                    LEFT JOIN {TableUser} u ON l.userId = u.id
                    LEFT JOIN {TableNode} n ON n.id = l.nodeId{logWhereClause}
                ) timeline ORDER BY timeline.SortDate DESC, timeline.Date DESC";

            var sql = scope.SqlContext.Sql(unionSql, parameters.ToArray());
            var pageNumber = CalculatePageNumber(filter.Skip, filter.Take);
            var page = scope.Database.Page<TimelineDto>(pageNumber, filter.Take, sql);

            return new AuditLogPagedResult<TimelineEntryViewModel>
            {
                Items = page.Items.Select(MapToTimelineEntryViewModel),
                Total = page.TotalItems
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching timeline");
            return new() { Items = [], Total = 0 };
        }
    }

    private static TimelineEntryViewModel MapToTimelineEntryViewModel(TimelineDto dto) => new()
    {
        Date = dto.Source == "audit"
            ? DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc)
            : dto.Date,
        Source = dto.Source ?? "",
        UserId = dto.UserId,
        User = FormatUserDisplay(dto.User, dto.UserEmail)
            ?? dto.User ?? "",
        Action = dto.Action ?? "",
        Details = dto.Details ?? "",
        Extra = dto.Extra ?? "",
        NodeId = dto.NodeId,
        NodeName = dto.NodeName ?? ""
    };

    #endregion

    #region Lookups

    public IEnumerable<string> GetDistinctEventTypes()
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.Fetch<string>(
                scope.SqlContext.Sql().Select("DISTINCT eventType").From(TableAudit));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching event types");
            return [];
        }
    }

    public IEnumerable<string> GetDistinctLogHeaders()
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.Fetch<string>(
                scope.SqlContext.Sql().Select("DISTINCT logHeader").From(TableLog));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching log headers");
            return [];
        }
    }

    public IEnumerable<UserInfoViewModel> GetUsers()
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var sql = scope.SqlContext.Sql()
                .Select("id, userName, userEmail")
                .From(TableUser)
                .OrderBy("userName");

            return scope.Database.Fetch<UserDto>(sql)
                .Select(u => new UserInfoViewModel
                {
                    Id = u.Id,
                    Name = u.UserName ?? "",
                    Email = u.UserEmail ?? ""
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching users");
            return [];
        }
    }

    #endregion

    #region Helpers

    private static int CalculatePageNumber(int skip, int take)
        => skip / Math.Max(take, 1) + 1;

    private static string? FormatUserDisplay(string? userName, string? userEmail)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        return string.IsNullOrWhiteSpace(userEmail)
            ? userName
            : $"{userName} <{userEmail}>";
    }

    private static void AddCondition(List<string> conditions, List<object> parameters,
        ref int paramIndex, string? value, string conditionTemplate)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        conditions.Add(conditionTemplate);
        parameters.Add(value);
        paramIndex++;
    }

    private static void AddCondition(List<string> conditions, List<object> parameters,
        ref int paramIndex, int? value, string conditionTemplate)
    {
        if (!value.HasValue) return;
        conditions.Add(conditionTemplate);
        parameters.Add(value.Value);
        paramIndex++;
    }

    private static void AddSearchCondition(List<string> conditions, List<object> parameters,
        ref int paramIndex, string? searchTerm, params string[] columns)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return;
        var idx = paramIndex;
        var likeConditions = string.Join(" OR ", columns.Select(c => $"{c} LIKE @{idx}"));
        conditions.Add($"({likeConditions})");
        parameters.Add($"%{searchTerm}%");
        paramIndex++;
    }

    private static void AddDateRange(List<string> conditions, List<object> parameters,
        ref int paramIndex, DateTime? dateFrom, DateTime? dateTo, string column)
    {
        if (dateFrom.HasValue)
        {
            conditions.Add($"{column} >= @{paramIndex}");
            parameters.Add(dateFrom.Value);
            paramIndex++;
        }

        if (dateTo.HasValue)
        {
            conditions.Add($"{column} <= @{paramIndex}");
            parameters.Add(dateTo.Value);
            paramIndex++;
        }
    }

    #endregion
}
