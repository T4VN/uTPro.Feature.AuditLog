using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

internal partial class AuditLogService
{
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
}
