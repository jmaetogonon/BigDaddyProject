using System.ComponentModel.DataAnnotations;

namespace BigDaddyProject.Application.DTOs;

//public class TeamDtos
//{
//    public record CreateTeamRequest(
//    [Required] string ProjectId,
//    [Required][MaxLength(200)] string Name
//);

//    public record UpdateTeamRequest([Required][MaxLength(200)] string Name);

//    public record TeamSummaryDto(int Id, string ProjectId, string Name);

//    public record TeamDetailDto(
//        int Id,
//        string ProjectId,
//        string Name,
//        List<UserListDto> Users,
//        List<RoleSummaryDto> Roles
//    );

//    public record AssignUsersToTeamRequest(List<int> UserIds);
//}

public record CreateTeamRequest(
    [Required] string ProjectId,
    [Required][MaxLength(200)] string Name
);

public record UpdateTeamRequest([Required][MaxLength(200)] string Name);

public record TeamSummaryDto(int Id, string ProjectId, string Name);

public record TeamDetailDto(
    int Id,
    string ProjectId,
    string Name,
    List<UserListDto> Users,
    List<RoleSummaryDto> Roles
);

public record AssignUsersToTeamRequest(List<int> UserIds);