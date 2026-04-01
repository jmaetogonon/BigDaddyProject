using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize(Roles = "SystemAdministrator,Manager")]
[Produces("application/json")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teams;

    public TeamsController(ITeamService teams) => _teams = teams;

    /// <summary>Get all teams, optionally filtered by projectId</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TeamSummaryDto>), 200)]
    public async Task<IActionResult> GetTeams([FromQuery] string? projectId = null)
        => Ok(await _teams.GetTeamsAsync(projectId));

    /// <summary>Get team by ID with users and roles</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeamDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTeam(int id)
    {
        var team = await _teams.GetTeamByIdAsync(id);
        return team == null
            ? NotFound(new { message = "Team not found." })
            : Ok(team);
    }

    /// <summary>Create a new team</summary>
    [HttpPost]
    [Authorize(Roles = "SystemAdministrator")]
    [ProducesResponseType(typeof(TeamSummaryDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _teams.CreateTeamAsync(request);
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetTeam), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>Update team name</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdministrator")]
    public async Task<IActionResult> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _teams.UpdateTeamAsync(id, request);
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(result.Data);
    }

    /// <summary>Assign users to a team (replaces existing assignments)</summary>
    [HttpPost("{id:int}/assign-users")]
    public async Task<IActionResult> AssignUsers(
        int id, [FromBody] AssignUsersToTeamRequest request)
    {
        var result = await _teams.AssignUsersAsync(id, request);
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Users assigned to team." });
    }
}