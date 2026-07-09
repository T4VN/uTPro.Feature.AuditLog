namespace uTPro.Feature.AuditLog.Models;

internal class AuditEntryDto
{
    public int Id { get; set; }
    public int PerformingUserId { get; set; }
    public string? PerformingDetails { get; set; }
    public string? PerformingIp { get; set; }
    public DateTime EventDateUtc { get; set; }
    public string? EventType { get; set; }
    public string? EventDetails { get; set; }
    public int AffectedUserId { get; set; }
    public string? AffectedDetails { get; set; }
    // Joined fields
    public string? PerformingUserName { get; set; }
    public string? PerformingEmail { get; set; }
    public string? AffectedUserName { get; set; }
    public string? AffectedEmail { get; set; }
}
