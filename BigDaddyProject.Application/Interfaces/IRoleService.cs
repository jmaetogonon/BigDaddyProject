using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;

namespace BigDaddyProject.Application.Interfaces;

public interface IRoleService
{
    Task<ServiceResult<RoleSummaryDto>> CreateRoleAsync(CreateRoleRequest request);
    Task<ServiceResult<RoleSummaryDto>> UpdateRoleAsync(int roleId, UpdateRoleRequest request);
    Task<ServiceResult> AssignPermissionsAsync(int roleId, AssignPermissionsToRoleRequest request);
    Task<List<RoleSummaryDto>> GetRolesAsync(string? projectId);
    Task<RoleDetailDto?> GetRoleByIdAsync(int roleId);
}