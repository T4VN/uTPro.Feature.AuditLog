namespace uTPro.Feature.AuditLog.Models;

public class LogEntryViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime DateStamp { get; set; }
    public string LogHeader { get; set; } = string.Empty;
    public string LogComment { get; set; } = string.Empty;
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? NodeKey { get; set; }
}
