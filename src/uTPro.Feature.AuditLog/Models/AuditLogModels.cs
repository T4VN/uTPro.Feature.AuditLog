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
}

public class AuditLogPagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Total { get; set; }
}

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
}

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
}

public class UserInfoViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

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
}

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
    public DateTime SortDate { get; set; }
}

internal class UserDto
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
}
