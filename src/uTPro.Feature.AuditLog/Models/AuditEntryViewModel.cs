namespace uTPro.Feature.AuditLog.Models;

public class AuditEntryViewModel
{
    public int Id { get; set; }
    public DateTime EventDateUtc { get; set; }
    public int PerformingUserId { get; set; }
    public string PerformingDetails { get; set; } = string.Empty;
    public string PerformingIp { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventDetails { get; set; } = string.Empty;
    public int AffectedUserId { get; set; }
    public string AffectedDetails { get; set; } = string.Empty;
}
