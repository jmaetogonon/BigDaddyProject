using Microsoft.AspNetCore.Identity;

namespace BigDaddyProject.Domain.Entities.Identity;

public class ApplicationRole : IdentityRole<int>
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TeamRole> TeamRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
