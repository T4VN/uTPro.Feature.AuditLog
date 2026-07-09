namespace uTPro.Feature.AuditLog.Models;

internal class LogEntryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? Text { get; set; }
    public DateTime DateStamp { get; set; }
    public string? LogHeader { get; set; }
    public string? LogComment { get; set; }
    public int NodeId { get; set; }
    public string? EntityType { get; set; }
    public Guid? NodeKey { get; set; }
}
