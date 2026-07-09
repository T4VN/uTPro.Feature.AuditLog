namespace uTPro.Feature.AuditLog.Models;

public class TimelineEntryViewModel
{
    public DateTime Date { get; set; }
    public string Source { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Extra { get; set; } = string.Empty;
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public Guid? NodeKey { get; set; }
}
