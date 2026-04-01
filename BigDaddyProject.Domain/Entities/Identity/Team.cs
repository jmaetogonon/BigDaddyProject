namespace BigDaddyProject.Domain.Entities.Identity;

public class Team
{
    public int Id { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AgentTeam> AgentTeams { get; set; } = [];
    public ICollection<TeamRole> TeamRoles { get; set; } = [];
}
