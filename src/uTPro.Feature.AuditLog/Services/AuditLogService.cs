using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

/// <summary>
/// Read-only query service over the <c>umbracoAudit</c> and <c>umbracoLog</c> tables.
/// The implementation is split across several partial files for readability, all sharing
/// the primary-constructor dependencies declared here:
/// <list type="bullet">
///   <item><description>AuditLogService.cs — audit-entries query + shared constants.</description></item>
///   <item><description>AuditLogService.LogEntries.cs — content-log query.</description></item>
///   <item><description>AuditLogService.Timeline.cs — unified UNION timeline query.</description></item>
///   <item><description>AuditLogService.Lookups.cs — distinct event types / log headers / users.</description></item>
///   <item><description>AuditLogService.SqlHelpers.cs — SQL building, whitelisted sorting, escaping.</description></item>
/// </list>
/// </summary>
internal partial class AuditLogService(IScopeProvider scopeProvider, ILogger<AuditLogService> logger) : IAuditLogService
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
}
