namespace Platform.Domain.Registration;

public enum RegistrationState { PendingReview, Approved, Rejected }

public sealed class RegistrationApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public RegistrationState State { get; set; } = RegistrationState.PendingReview;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? PublicReason { get; set; }
    public string? InternalNote { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
