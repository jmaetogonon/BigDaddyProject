using System.ComponentModel.DataAnnotations;

namespace BigDaddyProject.Application.DTOs;

//public class RoleDtos
//{
//    public record CreateRoleRequest(
//    [Required] string ProjectId,
//    [Required][MaxLength(200)] string Name
//);

//    public record UpdateRoleRequest([Required][MaxLength(200)] string Name);

//    public record RoleSummaryDto(int Id, string Name, string ProjectId);

//    public record RoleDetailDto(
//        int Id,
//        string Name,
//        string ProjectId,
//        List<RolePermissionDto> Permissions
//    );

//    public record RolePermissionDto(
//        int PermissionId,
//        string PermissionName,
//        string Group,
//        int AccessLevel
//    );

//    public record AssignPermissionsToRoleRequest(
//        List<RolePermissionAssignmentDto> Permissions
//    );

//    public record RolePermissionAssignmentDto(int PermissionId, int AccessLevel);
//}


public record CreateRoleRequest(
    [Required] string ProjectId,
    [Required][MaxLength(200)] string Name
);

public record UpdateRoleRequest([Required][MaxLength(200)] string Name);

public record RoleSummaryDto(int Id, string Name, string ProjectId);

public record RoleDetailDto(
    int Id,
    string Name,
    string ProjectId,
    List<RolePermissionDto> Permissions
);

public record RolePermissionDto(
    int PermissionId,
    string PermissionName,
    string Group,
    int AccessLevel
);

public record AssignPermissionsToRoleRequest(List<RolePermissionAssignmentDto> Permissions);

public record RolePermissionAssignmentDto(int PermissionId, int AccessLevel);