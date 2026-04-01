using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Get paginated list of users</summary>
    [HttpGet]
    [Authorize(Roles = "SystemAdministrator,Manager")]
    [ProducesResponseType(typeof(PagedResult<UserListDto>), 200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await _users.GetUsersAsync(page, pageSize, search, status);
        return Ok(result);
    }

    /// <summary>Get user by ID</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "SystemAdministrator,Manager")]
    [ProducesResponseType(typeof(UserDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _users.GetUserByIdAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });
        return Ok(user);
    }

    /// <summary>Create a new user (Admin Portal only)</summary>
    [HttpPost]
    [Authorize(Roles = "SystemAdministrator,Manager")]
    [ProducesResponseType(typeof(UserDetailDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var createdBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _users.CreateUserAsync(request, createdBy);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>Update user details, teams, roles</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SystemAdministrator,Manager")]
    [ProducesResponseType(typeof(UserDetailDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _users.UpdateUserAsync(id, request);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Activate a user account</summary>
    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "SystemAdministrator,Manager")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _users.SetStatusAsync(id, "Active");
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "User activated." });
    }

    /// <summary>Deactivate a user account</summary>
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "SystemAdministrator,Manager")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _users.SetStatusAsync(id, "Inactive");
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "User deactivated." });
    }

    /// <summary>Admin-initiated password reset for a user</summary>
    [HttpPost("{id:int}/reset-password")]
    [Authorize(Roles = "SystemAdministrator,Manager")]
    public async Task<IActionResult> AdminResetPassword(
        int id, [FromBody] AdminResetPasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _users.AdminResetPasswordAsync(id, request);
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Password reset successfully." });
    }
}