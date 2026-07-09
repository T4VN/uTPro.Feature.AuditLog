using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

internal class AuditLogService(IScopeProvider scopeProvider, ILogger<AuditLogService> logger) : IAuditLogService
{
    private const string TableAudit = "umbracoAudit";
    private const string TableLog = "umbracoLog";
    private const string TableUser = "umbracoUser";
    private const string TableNode = "umbracoNode";
    private const string TableDictionary = "cmsDictionary";

    // umbracoLog.entityType value Umbraco writes for dictionary item audit entries
    // (Create/Update/Delete/Move DictionaryItem). Its NodeId is the dictionary item's
    // integer id, which maps to cmsDictionary.pk (not umbracoNode.id).
    private const string DictionaryEntityType = "DictionaryItem";

    // Offset (in minutes) to convert local server time → UTC: subtract the local offset.
    private static readonly int NegatedUtcOffsetMinutes =
        -(int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;

    #region Audit Entries

    public AuditLogPagedResult<AuditEntryViewModel> GetAuditEntries(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var syntax = scope.SqlContext.SqlSyntax;

        // Local helpers that produce provider-correct, case-safe identifiers
        // (e.g. au."eventType" on PostgreSQL, au.[eventType] on SQL Server).
        string Au(string c) => Col(syntax, "au", c);
        string P(string c) => Col(syntax, "p", c);
        string A(string c) => Col(syntax, "a", c);

        var sql = scope.SqlContext.Sql()
            .Select($@"au.*, 
                      {P("userName")} AS performingUserName, {P("userEmail")} AS performingEmail,
                      {A("userName")} AS affectedUserName, {A("userEmail")} AS affectedEmail")
            .From($"{syntax.GetQuotedTableName(TableAudit)} au")
            .LeftJoin($"{syntax.GetQuotedTableName(TableUser)} p").On($"{Au("performingUserId")} = {P("id")}")
            .LeftJoin($"{syntax.GetQuotedTableName(TableUser)} a").On($"{Au("affectedUserId")} = {A("id")}");

        var conditions = new List<string>();
        var parameters = new List<object>();
        var paramIndex = 0;

        AddCondition(conditions, parameters, ref paramIndex,
            filter.EventType, $"{Au("eventType")} = @{paramIndex}");

        AddCondition(conditions, parameters, ref paramIndex,
            filter.UserId, $"{Au("performingUserId")} = @{paramIndex}");

        AddCondition(conditions, parameters, ref paramIndex,
            filter.AffectedUserId, $"{Au("affectedUserId")} = @{paramIndex}");

        AddSearchCondition(conditions, parameters, ref paramIndex,
            filter.SearchTerm,
            Au("eventDetails"), Au("performingDetails"), Au("affectedDetails"),
            Au("performingIp"), Au("eventType"), P("userName"), A("userName"));

        AddDateRange(conditions, parameters, ref paramIndex,
            filter.DateFrom, filter.DateTo, Au("eventDateUtc"));

        if (conditions.Count > 0)
            sql = sql.Where(string.Join(" AND ", conditions), parameters.ToArray());

        sql = sql.OrderBy(BuildOrderBy(syntax, filter.SortColumn, filter.SortDirection,
            AuditSortColumns, $"{Au("eventDateUtc")} DESC, {Au("id")} DESC"));

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
        var syntax = scope.SqlContext.SqlSyntax;

        string L(string c) => Col(syntax, "l", c);
        string U(string c) => Col(syntax, "u", c);
        string N(string c) => Col(syntax, "n", c);
        string D(string c) => Col(syntax, "d", c);

        // For dictionary items the node lives in cmsDictionary (key + guid), not umbracoNode,
        // so resolve the display name / edit key from the matching source per entityType.
        var nodeName = $"CASE WHEN {L("entityType")} = '{DictionaryEntityType}' THEN {D("key")} ELSE {N("text")} END";
        var nodeKey = $"CASE WHEN {L("entityType")} = '{DictionaryEntityType}' THEN {D("id")} ELSE {N("uniqueId")} END";

        var sql = scope.SqlContext.Sql()
            .Select($@"{L("id")}, {L("userId")}, {L("Datestamp")}, {L("logHeader")}, {L("logComment")}, {L("NodeId")}, {L("entityType")},
                      CASE WHEN {U("userName")} IS NULL THEN 'SYSTEM' ELSE {U("userName")} END AS Username,
                      {U("userEmail")}, {nodeName} AS {syntax.GetQuotedColumnName("text")}, {nodeKey} AS NodeKey")
            .From($"{syntax.GetQuotedTableName(TableLog)} l")
            .LeftJoin($"{syntax.GetQuotedTableName(TableUser)} u").On($"{L("userId")} = {U("id")}")
            .LeftJoin($"{syntax.GetQuotedTableName(TableNode)} n").On($"{N("id")} = {L("NodeId")}")
            .LeftJoin($"{syntax.GetQuotedTableName(TableDictionary)} d").On($"{D("pk")} = {L("NodeId")}");

        var conditions = new List<string>();
        var parameters = new List<object>();
        var paramIndex = 0;

        AddCondition(conditions, parameters, ref paramIndex,
            filter.EventType, $"{L("logHeader")} = @{paramIndex}");

        AddCondition(conditions, parameters, ref paramIndex,
            filter.UserId, $"{L("userId")} = @{paramIndex}");

        AddSearchCondition(conditions, parameters, ref paramIndex,
            filter.SearchTerm,
            L("logComment"), U("userName"), L("entityType"), L("logHeader"),
            $"CAST({L("NodeId")} AS VARCHAR)");

        AddDateRange(conditions, parameters, ref paramIndex,
            filter.DateFrom, filter.DateTo, L("Datestamp"));

        if (conditions.Count > 0)
            sql = sql.Where(string.Join(" AND ", conditions), parameters.ToArray());

        sql = sql.OrderBy(BuildOrderBy(syntax, filter.SortColumn, filter.SortDirection,
            LogSortColumns, $"{L("Datestamp")} DESC, {L("id")} DESC"));

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
        NodeName = dto.Text ?? "",
        NodeKey = dto.NodeKey
    };

    #endregion

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
                auditConditions.Add($"({A("eventDetails")} LIKE @{paramIndex} OR {A("performingDetails")} LIKE @{paramIndex} OR {A("affectedDetails")} LIKE @{paramIndex} OR {A("eventType")} LIKE @{paramIndex})");
                logConditions.Add($"({L("logComment")} LIKE @{paramIndex} OR {L("logHeader")} LIKE @{paramIndex})");
                parameters.Add($"%{filter.SearchTerm}%");
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

    #region Lookups

    public IEnumerable<string> GetDistinctEventTypes()
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var syntax = scope.SqlContext.SqlSyntax;
            return scope.Database.Fetch<string>(
                scope.SqlContext.Sql()
                    .Select($"DISTINCT {syntax.GetQuotedColumnName("eventType")}")
                    .From(syntax.GetQuotedTableName(TableAudit)));
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
            var syntax = scope.SqlContext.SqlSyntax;
            return scope.Database.Fetch<string>(
                scope.SqlContext.Sql()
                    .Select($"DISTINCT {syntax.GetQuotedColumnName("logHeader")}")
                    .From(syntax.GetQuotedTableName(TableLog)));
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
            var syntax = scope.SqlContext.SqlSyntax;
            var sql = scope.SqlContext.Sql()
                .Select($"{syntax.GetQuotedColumnName("id")}, {syntax.GetQuotedColumnName("userName")}, {syntax.GetQuotedColumnName("userEmail")}")
                .From(syntax.GetQuotedTableName(TableUser))
                .OrderBy(syntax.GetQuotedColumnName("userName"));

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

    // Sort maps hold logical (alias, column) pairs; the column is quoted per-provider at
    // build time so ORDER BY is safe on SQL Server, SQLite and PostgreSQL alike.
    private static readonly IReadOnlyDictionary<string, (string Alias, string Column)> AuditSortColumns =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["date"] = ("au", "eventDateUtc"),
            ["user"] = ("p", "userName"),
            ["eventType"] = ("au", "eventType"),
            ["details"] = ("au", "eventDetails"),
            ["ip"] = ("au", "performingIp"),
            ["affected"] = ("au", "affectedDetails"),
        };

    private static readonly IReadOnlyDictionary<string, (string Alias, string Column)> LogSortColumns =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["date"] = ("l", "Datestamp"),
            ["user"] = ("u", "userName"),
            ["logHeader"] = ("l", "logHeader"),
            ["comment"] = ("l", "logComment"),
            ["nodeId"] = ("l", "NodeId"),
            ["nodeName"] = ("n", "text"),
            ["entity"] = ("l", "entityType"),
        };

    private static readonly IReadOnlyDictionary<string, (string Alias, string Column)> TimelineSortColumns =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["date"] = ("timeline", "SortDate"),
            ["user"] = ("timeline", "User"),
            ["source"] = ("timeline", "Source"),
            ["action"] = ("timeline", "Action"),
        };

    /// <summary>
    /// Produces a provider-correct, case-safe qualified identifier such as
    /// <c>au."eventType"</c> (PostgreSQL) or <c>au.[eventType]</c> (SQL Server).
    /// The table alias is emitted verbatim (aliases are chosen to be lower-case and safe).
    /// </summary>
    private static string Col(ISqlSyntaxProvider syntax, string alias, string column)
        => $"{alias}.{syntax.GetQuotedColumnName(column)}";

    /// <summary>
    /// Builds a safe ORDER BY clause from a whitelist. Falls back to the default clause
    /// when the requested column is not recognised (prevents SQL injection).
    /// </summary>
    private static string BuildOrderBy(ISqlSyntaxProvider syntax, string? sortColumn, string? sortDirection,
        IReadOnlyDictionary<string, (string Alias, string Column)> allowed, string defaultClause)
    {
        if (string.IsNullOrWhiteSpace(sortColumn) || !allowed.TryGetValue(sortColumn, out var target))
            return defaultClause;

        var direction = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC" : "DESC";

        return $"{Col(syntax, target.Alias, target.Column)} {direction}";
    }

    private static int CalculatePageNumber(int skip, int take)
        => skip / Math.Max(take, 1) + 1;

    /// <summary>
    /// Builds a provider-specific SQL expression that shifts a local-time column to UTC,
    /// so audit (UTC) and log (local) timestamps can be sorted on the same scale.
    /// </summary>
    private static string BuildLocalToUtcExpression(IScope scope, string column)
    {
        var providerName = scope.Database.DatabaseType.GetType().Name;
        var isSqlite = providerName.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
        var isPostgres = providerName.Contains("Postgre", StringComparison.OrdinalIgnoreCase)
            || providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

        if (isSqlite)
        {
            // SQLite: datetime(col, '+N minutes') — sign is required in the modifier.
            var signedMinutes = NegatedUtcOffsetMinutes.ToString("+0;-0");
            return $"datetime({column}, '{signedMinutes} minutes')";
        }

        if (isPostgres)
        {
            // PostgreSQL: col + (N * interval '1 minute'). DATEADD does not exist.
            return $"({column} + ({NegatedUtcOffsetMinutes} * interval '1 minute'))";
        }

        // SQL Server (and compatible providers): DATEADD(MINUTE, N, col).
        return $"DATEADD(MINUTE, {NegatedUtcOffsetMinutes}, {column})";
    }

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
