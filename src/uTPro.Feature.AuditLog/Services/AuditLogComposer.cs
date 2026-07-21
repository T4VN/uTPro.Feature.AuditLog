using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uTPro.Feature.AuditLog.Services;

internal class AuditLogComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register a dedicated Swagger document for this feature (its own dropdown entry).
        builder.Services.ConfigureOptions<ConfigureAuditLogSwaggerGenOptions>();

        builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    }
}
