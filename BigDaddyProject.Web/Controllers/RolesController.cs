using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "SystemAdministrator")]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles) => _roles = roles;

    /// <summary>Get all roles, optionally filtered by projectId</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleSummaryDto>), 200)]
    public async Task<IActionResult> GetRoles([FromQuery] string? projectId = null)
        => Ok(await _roles.GetRolesAsync(projectId));

    /// <summary>Get role by ID with permissions</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RoleDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRole(int id)
    {
        var role = await _roles.GetRoleByIdAsync(id);
        return role == null
            ? NotFound(new { message = "Role not found." })
            : Ok(role);
    }

    /// <summary>Create a new role</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleSummaryDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _roles.CreateRoleAsync(request);
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetRole), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>Update role name</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _roles.UpdateRoleAsync(id, request);
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(result.Data);
    }

    /// <summary>
    /// Assign permissions to a role (replaces existing).
    /// Send access level: 0=None, 1=Individual, 2=Organization
    /// </summary>
    [HttpPost("{id:int}/permissions")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AssignPermissions(
        int id, [FromBody] AssignPermissionsToRoleRequest request)
    {
        var result = await _roles.AssignPermissionsAsync(id, request);
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Permissions assigned successfully." });
    }
}