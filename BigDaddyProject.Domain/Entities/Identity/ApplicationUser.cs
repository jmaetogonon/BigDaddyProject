using Microsoft.AspNetCore.Identity;

namespace BigDaddyProject.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;
    public string? NRICName { get; set; }
    public string? CEANumber { get; set; }
    public DateTime? CEAExpiry { get; set; }
    public string? Mobile { get; set; }
    public string? Gender { get; set; }
    public string? Photo { get; set; }
    public string Status { get; set; } = "Active"; // Active / Inactive
    public bool MustChangePassword { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<AgentTeam> AgentTeams { get; set; } = [];

    public ICollection<UserAuditLog> AuditLogs { get; set; } = [];
    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = [];
}