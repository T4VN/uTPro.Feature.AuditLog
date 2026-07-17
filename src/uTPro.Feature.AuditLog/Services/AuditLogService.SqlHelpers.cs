using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using Umbraco.Cms.Infrastructure.Scoping;

namespace uTPro.Feature.AuditLog.Services;

internal partial class AuditLogService
{
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
        // Escape LIKE wildcards in the (still parameterized) value and declare ESCAPE '\'
        // so user input cannot alter match semantics or cause pathological scans.
        var likeConditions = string.Join(" OR ", columns.Select(c => $"{c} LIKE @{idx} ESCAPE '\\'"));
        conditions.Add($"({likeConditions})");
        parameters.Add($"%{EscapeLikePattern(searchTerm)}%");
        paramIndex++;
    }

    /// <summary>
    /// Escapes SQL LIKE metacharacters (%, _, [) in a user-supplied search term using a
    /// backslash escape character. The backslash itself is escaped first. The result is
    /// still bound as a parameter — this only neutralises wildcards, it does not build SQL.
    /// Pair with an <c>ESCAPE '\'</c> clause on the LIKE fragment.
    /// </summary>
    private static string EscapeLikePattern(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[");

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
