using BigDaddyProject.Domain.Enums;

namespace BigDaddyProject.Domain.Entities.Identity;

public class UserAuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AuditOperation Operation { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
