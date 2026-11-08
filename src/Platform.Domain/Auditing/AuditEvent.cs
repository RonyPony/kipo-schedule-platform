namespace Platform.Domain.Auditing;

public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorUserId { get; set; }
    public Guid? BusinessId { get; set; }
    public string Action { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public Guid ResourceId { get; set; }
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
