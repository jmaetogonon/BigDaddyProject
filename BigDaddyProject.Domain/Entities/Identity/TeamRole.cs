namespace BigDaddyProject.Domain.Entities.Identity;

public class TeamRole
{
    public int TeamId { get; set; }
    public int RoleId { get; set; }

    // Navigation
    public Team Team { get; set; } = null!;
    public ApplicationRole Role { get; set; } = null!;
}
