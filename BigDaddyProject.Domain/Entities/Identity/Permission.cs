using BigDaddyProject.Domain.Enums;

namespace BigDaddyProject.Domain.Entities.Identity;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PermissionType Type { get; set; }
    public string Group { get; set; } = string.Empty; // General, Pricing, Booking, etc.
    public int DisplayOrder { get; set; }

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
