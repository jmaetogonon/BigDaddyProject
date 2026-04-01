namespace BigDaddyProject.Domain.Entities.Identity;

public class AgentTeam
{
    public int UserId { get; set; }
    public int TeamId { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
