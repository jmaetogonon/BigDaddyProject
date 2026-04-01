namespace BigDaddyProject.Application.DTOs;

//public class PermissionDtos
//{
//    public record PermissionSummaryDto(
//int Id,
//string Name,
//string Type,
//string Group,
//int DisplayOrder
//);
//}


public record PermissionSummaryDto(
    int Id,
    string Name,
    string Type,
    string Group,
    int DisplayOrder
);