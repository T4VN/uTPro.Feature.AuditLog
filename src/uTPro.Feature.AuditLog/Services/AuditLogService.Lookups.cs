using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

internal partial class AuditLogService
{
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
}
