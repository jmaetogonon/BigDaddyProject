using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;

namespace BigDaddyProject.Application.Interfaces;

public interface ITeamService
{
    Task<ServiceResult<TeamSummaryDto>> CreateTeamAsync(CreateTeamRequest request);
    Task<ServiceResult<TeamSummaryDto>> UpdateTeamAsync(int teamId, UpdateTeamRequest request);
    Task<ServiceResult> AssignUsersAsync(int teamId, AssignUsersToTeamRequest request);
    Task<List<TeamSummaryDto>> GetTeamsAsync(string? projectId);
    Task<TeamDetailDto?> GetTeamByIdAsync(int teamId);
}