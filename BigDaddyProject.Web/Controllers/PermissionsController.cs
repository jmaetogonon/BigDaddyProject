using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "SystemAdministrator")]
[Produces("application/json")]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PermissionsController(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Get all permissions, optionally filtered by group.
    /// Returns grouped list for the role-permissions assignment UI.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionSummaryDto>), 200)]
    public async Task<IActionResult> GetPermissions([FromQuery] string? group = null)
    {
        var query = _db.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(group))
            query = query.Where(p => p.Group == group);

        var permissions = await query
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PermissionSummaryDto(
                p.Id,
                p.Name,
                p.Type.ToString(),
                p.Group,
                p.DisplayOrder))
            .ToListAsync();

        return Ok(permissions);
    }

    /// <summary>Get all permissions grouped by Group name — useful for the UI matrix</summary>
    [HttpGet("grouped")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetGrouped()
    {
        var permissions = await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PermissionSummaryDto(
                p.Id, p.Name, p.Type.ToString(), p.Group, p.DisplayOrder))
            .ToListAsync();

        var grouped = permissions
            .GroupBy(p => p.Group)
            .Select(g => new
            {
                Group = g.Key,
                Permissions = g.ToList()
            })
            .ToList();

        return Ok(grouped);
    }
}
