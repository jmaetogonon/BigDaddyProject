using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Entities.Identity;
using BigDaddyProject.Domain.Enums;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public RoleService(RoleManager<ApplicationRole> roleManager, ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<ServiceResult<RoleSummaryDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        if (await _db.Roles.AnyAsync(r =>
                r.ProjectId == request.ProjectId && r.Name == request.Name))
            return ServiceResult<RoleSummaryDto>.Fail("Role name already exists in this project.");

        var role = new ApplicationRole
        {
            Name = request.Name,
            NormalizedName = request.Name.ToUpper(),
            ProjectId = request.ProjectId
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return ServiceResult<RoleSummaryDto>.Fail(
                string.Join(", ", result.Errors.Select(e => e.Description)));

        return ServiceResult<RoleSummaryDto>.Ok(
            new RoleSummaryDto(role.Id, role.Name!, role.ProjectId));
    }

    public async Task<ServiceResult<RoleSummaryDto>> UpdateRoleAsync(
        int roleId, UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null) return ServiceResult<RoleSummaryDto>.Fail("Role not found.");

        role.Name = request.Name;
        role.NormalizedName = request.Name.ToUpper();
        await _roleManager.UpdateAsync(role);

        return ServiceResult<RoleSummaryDto>.Ok(
            new RoleSummaryDto(role.Id, role.Name!, role.ProjectId));
    }

    public async Task<ServiceResult> AssignPermissionsAsync(
        int roleId, AssignPermissionsToRoleRequest request)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == roleId))
            return ServiceResult.Fail("Role not found.");

        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);

        foreach (var perm in request.Permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = perm.PermissionId,
                AccessLevel = (AccessLevel)perm.AccessLevel
            });
        }

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<List<RoleSummaryDto>> GetRolesAsync(string? projectId)
    {
        var query = _db.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(projectId))
            query = query.Where(r => r.ProjectId == projectId);

        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RoleSummaryDto(r.Id, r.Name!, r.ProjectId))
            .ToListAsync();
    }

    public async Task<RoleDetailDto?> GetRoleByIdAsync(int roleId)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null) return null;

        return new RoleDetailDto(
            role.Id, role.Name!, role.ProjectId,
            role.RolePermissions
                .OrderBy(rp => rp.Permission.Group)
                .ThenBy(rp => rp.Permission.DisplayOrder)
                .Select(rp => new RolePermissionDto(
                    rp.PermissionId, rp.Permission.Name,
                    rp.Permission.Group, (int)rp.AccessLevel))
                .ToList());
    }
}