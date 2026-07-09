namespace uTPro.Feature.AuditLog.Models;

public class AuditLogPagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Total { get; set; }
}
