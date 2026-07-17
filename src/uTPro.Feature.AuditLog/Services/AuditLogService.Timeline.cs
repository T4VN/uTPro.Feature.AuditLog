using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

internal partial class AuditLogService
{
    #region Timeline

    public AuditLogPagedResult<TimelineEntryViewModel> GetTimeline(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var syntax = scope.SqlContext.SqlSyntax;

        string A(string c) => Col(syntax, "a", c);
        string Pu(string c) => Col(syntax, "pu", c);
        string L(string c) => Col(syntax, "l", c);
        string U(string c) => Col(syntax, "u", c);
        string N(string c) => Col(syntax, "n", c);
        string D(string c) => Col(syntax, "d", c);
        string Alias(string c) => syntax.GetQuotedColumnName(c);

        try
        {
            var parameters = new List<object>();
            var paramIndex = 0;
            var auditConditions = new List<string>();
            var logConditions = new List<string>();

            if (filter.UserId.HasValue)
            {
                auditConditions.Add($"{A("performingUserId")} = @{paramIndex}");
                logConditions.Add($"{L("userId")} = @{paramIndex}");
                parameters.Add(filter.UserId.Value);
                paramIndex++;
            }

            if (filter.AffectedUserId.HasValue)
            {
                auditConditions.Add($"{A("affectedUserId")} = @{paramIndex}");
                parameters.Add(filter.AffectedUserId.Value);
                paramIndex++;
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                // Escape LIKE wildcards in the (parameterized) term and add ESCAPE '\' so the
                // user's search cannot change match semantics or trigger pathological scans.
                auditConditions.Add($"({A("eventDetails")} LIKE @{paramIndex} ESCAPE '\\' OR {A("performingDetails")} LIKE @{paramIndex} ESCAPE '\\' OR {A("affectedDetails")} LIKE @{paramIndex} ESCAPE '\\' OR {A("eventType")} LIKE @{paramIndex} ESCAPE '\\')");
                logConditions.Add($"({L("logComment")} LIKE @{paramIndex} ESCAPE '\\' OR {L("logHeader")} LIKE @{paramIndex} ESCAPE '\\')");
                parameters.Add($"%{EscapeLikePattern(filter.SearchTerm)}%");
                paramIndex++;
            }

            if (filter.DateFrom.HasValue)
            {
                auditConditions.Add($"{A("eventDateUtc")} >= @{paramIndex}");
                logConditions.Add($"{L("Datestamp")} >= @{paramIndex}");
                parameters.Add(filter.DateFrom.Value);
                paramIndex++;
            }

            if (filter.DateTo.HasValue)
            {
                auditConditions.Add($"{A("eventDateUtc")} <= @{paramIndex}");
                logConditions.Add($"{L("Datestamp")} <= @{paramIndex}");
                parameters.Add(filter.DateTo.Value);
                paramIndex++;
            }

            var auditWhereClause = auditConditions.Count > 0
                ? " WHERE " + string.Join(" AND ", auditConditions) : "";
            var logWhereClause = logConditions.Count > 0
                ? " WHERE " + string.Join(" AND ", logConditions) : "";

            // umbracoLog.Datestamp is stored in local server time, while umbracoAudit.eventDateUtc
            // is UTC. Convert the log timestamp to UTC so both sources sort consistently.
            // Use the correct date-arithmetic syntax for the active database provider.
            var logSortDateExpr = BuildLocalToUtcExpression(scope, L("Datestamp"));

            // Dictionary items are not umbracoNode rows: resolve their name/key from cmsDictionary.
            var logNodeName = $"CASE WHEN {L("entityType")} = '{DictionaryEntityType}' THEN {D("key")} ELSE {N("text")} END";
            var logNodeKey = $"CASE WHEN {L("entityType")} = '{DictionaryEntityType}' THEN {D("id")} ELSE {N("uniqueId")} END";

            var orderBy = BuildOrderBy(syntax, filter.SortColumn, filter.SortDirection,
                TimelineSortColumns, $"{Col(syntax, "timeline", "SortDate")} DESC, {Col(syntax, "timeline", "Date")} DESC");

            var unionSql = $@"
                SELECT * FROM (
                    SELECT {A("eventDateUtc")} AS {Alias("Date")}, 'audit' AS {Alias("Source")}, {A("performingUserId")} AS {Alias("UserId")},
                           CASE WHEN {Pu("userName")} IS NULL THEN {A("performingDetails")} ELSE {Pu("userName")} END AS {Alias("User")},
                           {Pu("userEmail")} AS {Alias("UserEmail")},
                           {A("eventType")} AS {Alias("Action")},
                           {A("affectedDetails")} AS {Alias("Details")},
                           {A("eventDetails")} AS {Alias("Extra")},
                           0 AS {Alias("NodeId")}, NULL AS {Alias("NodeName")}, NULL AS {Alias("NodeKey")},
                           {A("eventDateUtc")} AS {Alias("SortDate")}
                    FROM {syntax.GetQuotedTableName(TableAudit)} a
                    LEFT JOIN {syntax.GetQuotedTableName(TableUser)} pu ON {A("performingUserId")} = {Pu("id")}{auditWhereClause}
                    UNION ALL
                    SELECT {L("Datestamp")} AS {Alias("Date")}, 'log' AS {Alias("Source")}, {L("userId")} AS {Alias("UserId")},
                           CASE WHEN {U("userName")} IS NULL THEN 'SYSTEM' ELSE {U("userName")} END AS {Alias("User")},
                           {U("userEmail")} AS {Alias("UserEmail")},
                           {L("logHeader")} AS {Alias("Action")}, {L("logComment")} AS {Alias("Details")}, {L("entityType")} AS {Alias("Extra")},
                           {L("NodeId")} AS {Alias("NodeId")}, {logNodeName} AS {Alias("NodeName")}, {logNodeKey} AS {Alias("NodeKey")},
                           {logSortDateExpr} AS {Alias("SortDate")}
                    FROM {syntax.GetQuotedTableName(TableLog)} l
                    LEFT JOIN {syntax.GetQuotedTableName(TableUser)} u ON {L("userId")} = {U("id")}
                    LEFT JOIN {syntax.GetQuotedTableName(TableNode)} n ON {N("id")} = {L("NodeId")}
                    LEFT JOIN {syntax.GetQuotedTableName(TableDictionary)} d ON {D("pk")} = {L("NodeId")}{logWhereClause}
                ) timeline ORDER BY {orderBy}";

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
        NodeName = dto.NodeName ?? "",
        NodeKey = dto.NodeKey
    };

    #endregion
}
