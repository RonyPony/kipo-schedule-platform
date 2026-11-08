namespace Platform.Domain.Businesses;

public enum BusinessState { PendingApproval, Active, Rejected, Suspended, Archived }
public enum MembershipState { Pending, Active, Revoked }
public enum BusinessRole { Owner, Manager }

public sealed class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Activity { get; set; } = "";
    public string Phone { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string TimeZoneId { get; set; } = "America/Santo_Domingo";
    public DayOfWeek WeekStartsOn { get; set; } = DayOfWeek.Monday;
    public int SlotMinutes { get; set; } = 15;
    public BusinessState State { get; set; } = BusinessState.PendingApproval;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class BusinessMembership
{
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public BusinessRole Role { get; set; }
    public MembershipState State { get; set; } = MembershipState.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
