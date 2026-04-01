using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Entities.Identity;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly ApplicationDbContext _db;

    public TeamService(ApplicationDbContext db) => _db = db;

    public async Task<ServiceResult<TeamSummaryDto>> CreateTeamAsync(CreateTeamRequest request)
    {
        if (await _db.Teams.AnyAsync(t =>
                t.ProjectId == request.ProjectId && t.Name == request.Name))
            return ServiceResult<TeamSummaryDto>.Fail("A team with this name already exists in the project.");

        var team = new Team { ProjectId = request.ProjectId, Name = request.Name };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        return ServiceResult<TeamSummaryDto>.Ok(
            new TeamSummaryDto(team.Id, team.ProjectId, team.Name));
    }

    public async Task<ServiceResult<TeamSummaryDto>> UpdateTeamAsync(
        int teamId, UpdateTeamRequest request)
    {
        var team = await _db.Teams.FindAsync(teamId);
        if (team == null) return ServiceResult<TeamSummaryDto>.Fail("Team not found.");

        team.Name = request.Name;
        await _db.SaveChangesAsync();

        return ServiceResult<TeamSummaryDto>.Ok(
            new TeamSummaryDto(team.Id, team.ProjectId, team.Name));
    }

    public async Task<ServiceResult> AssignUsersAsync(int teamId, AssignUsersToTeamRequest request)
    {
        if (!await _db.Teams.AnyAsync(t => t.Id == teamId))
            return ServiceResult.Fail("Team not found.");

        var existing = await _db.AgentTeams.Where(at => at.TeamId == teamId).ToListAsync();
        _db.AgentTeams.RemoveRange(existing);

        foreach (var userId in request.UserIds.Distinct())
            _db.AgentTeams.Add(new AgentTeam { TeamId = teamId, UserId = userId });

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<List<TeamSummaryDto>> GetTeamsAsync(string? projectId)
    {
        var query = _db.Teams.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(projectId))
            query = query.Where(t => t.ProjectId == projectId);

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new TeamSummaryDto(t.Id, t.ProjectId, t.Name))
            .ToListAsync();
    }

    public async Task<TeamDetailDto?> GetTeamByIdAsync(int teamId)
    {
        var team = await _db.Teams
            .AsNoTracking()
            .Include(t => t.AgentTeams).ThenInclude(at => at.User)
            .Include(t => t.TeamRoles).ThenInclude(tr => tr.Role)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null) return null;

        return new TeamDetailDto(
            team.Id, team.ProjectId, team.Name,
            team.AgentTeams.Select(at => new UserListDto(
                at.User.Id, at.User.Name, at.User.Email!, at.User.Mobile,
                at.User.Status, new List<string>(), new List<string>(),
                at.User.CreatedAt)).ToList(),
            team.TeamRoles.Select(tr =>
                new RoleSummaryDto(tr.Role.Id, tr.Role.Name!, tr.Role.ProjectId)).ToList());
    }
}
