namespace uTPro.Feature.AuditLog.Models;

internal class TimelineDto
{
    public DateTime Date { get; set; }
    public string? Source { get; set; }
    public int UserId { get; set; }
    public string? User { get; set; }
    public string? UserEmail { get; set; }
    public string? Action { get; set; }
    public string? Details { get; set; }
    public string? Extra { get; set; }
    public int NodeId { get; set; }
    public string? NodeName { get; set; }
    public Guid? NodeKey { get; set; }
    public DateTime SortDate { get; set; }
}
