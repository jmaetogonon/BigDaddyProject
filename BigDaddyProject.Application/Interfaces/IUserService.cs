using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;

namespace BigDaddyProject.Application.Interfaces;

public interface IUserService
{
    Task<ServiceResult<UserDetailDto>> CreateUserAsync(CreateUserRequest request, int createdByUserId);
    Task<ServiceResult<UserDetailDto>> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<ServiceResult> SetStatusAsync(int userId, string status);
    Task<ServiceResult> AdminResetPasswordAsync(int userId, AdminResetPasswordRequest request);
    Task<PagedResult<UserListDto>> GetUsersAsync(int page, int pageSize, string? search, string? status);
    Task<UserDetailDto?> GetUserByIdAsync(int userId);
}