namespace Platform.Domain.Notifications;

public enum OutboxState { Pending, Processing, Sent, Failed }

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BusinessId { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public string Recipient { get; set; } = "";
    public string DeduplicationKey { get; set; } = "";
    public OutboxState State { get; set; } = OutboxState.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? LeaseId { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastErrorCode { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
