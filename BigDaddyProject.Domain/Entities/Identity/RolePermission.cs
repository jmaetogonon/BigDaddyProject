using BigDaddyProject.Domain.Enums;

namespace BigDaddyProject.Domain.Entities.Identity;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public AccessLevel AccessLevel { get; set; } = AccessLevel.None;

    // Navigation
    public ApplicationRole Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
