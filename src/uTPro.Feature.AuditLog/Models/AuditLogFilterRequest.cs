namespace uTPro.Feature.AuditLog.Models;

public class AuditLogFilterRequest
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string? EventType { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? UserId { get; set; }
    public int? AffectedUserId { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
}
