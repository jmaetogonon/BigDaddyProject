using System.ComponentModel.DataAnnotations;

namespace BigDaddyProject.Application.DTOs;

//public class UserDtos
//{
//    public record CreateUserRequest(
//    [Required][MaxLength(250)] string Name,
//    string? NRICName,
//    string? CEANumber,
//    DateTime? CEAExpiry,
//    string? Mobile,
//    [Required][EmailAddress] string Email,
//    string? Gender,
//    [Required] string Status,
//    bool MustChangePassword,
//    string? InitialPassword,   // null = auto-generate
//    List<int> TeamIds,
//    List<int> RoleIds
//);

//    public record UpdateUserRequest(
//        [Required][MaxLength(250)] string Name,
//        string? NRICName,
//        string? CEANumber,
//        DateTime? CEAExpiry,
//        string? Mobile,
//        [Required][EmailAddress] string Email,
//        string? Gender,
//        [Required] string Status,
//        List<int> TeamIds,
//        List<int> RoleIds
//    );

//    public record UserListDto(
//        int Id,
//        string Name,
//        string Email,
//        string? Mobile,
//        string Status,
//        List<string> Teams,
//        List<string> Roles,
//        DateTime CreatedAt
//    );

//    public record UserDetailDto(
//        int Id,
//        string Name,
//        string? NRICName,
//        string? CEANumber,
//        DateTime? CEAExpiry,
//        string? Mobile,
//        string Email,
//        string? Gender,
//        string? Photo,
//        string Status,
//        bool MustChangePassword,
//        DateTime CreatedAt,
//        DateTime? UpdatedAt,
//        List<TeamSummaryDto> Teams,
//        List<RoleSummaryDto> Roles
//    );

//    public record AdminResetPasswordRequest([Required] string NewPassword, bool MustChangePassword = true);
//}

public record CreateUserRequest(
    [Required][MaxLength(250)] string Name,
    string? NRICName,
    string? CEANumber,
    DateTime? CEAExpiry,
    string? Mobile,
    [Required][EmailAddress] string Email,
    string? Gender,
    [Required] string Status,
    bool MustChangePassword,
    string? InitialPassword,
    List<int> TeamIds,
    List<int> RoleIds
);

public record UpdateUserRequest(
    [Required][MaxLength(250)] string Name,
    string? NRICName,
    string? CEANumber,
    DateTime? CEAExpiry,
    string? Mobile,
    [Required][EmailAddress] string Email,
    string? Gender,
    [Required] string Status,
    List<int> TeamIds,
    List<int> RoleIds
);

public record UserListDto(
    int Id,
    string Name,
    string Email,
    string? Mobile,
    string Status,
    List<string> Teams,
    List<string> Roles,
    DateTime CreatedAt
);

public record UserDetailDto(
    int Id,
    string Name,
    string? NRICName,
    string? CEANumber,
    DateTime? CEAExpiry,
    string? Mobile,
    string Email,
    string? Gender,
    string? Photo,
    string Status,
    bool MustChangePassword,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<TeamSummaryDto> Teams,
    List<RoleSummaryDto> Roles
);

public record AdminResetPasswordRequest(
    [Required][MinLength(8)] string NewPassword,
    bool MustChangePassword = true
);