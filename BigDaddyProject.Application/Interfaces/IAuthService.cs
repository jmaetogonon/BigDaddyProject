using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;

namespace BigDaddyProject.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress);
    Task<ServiceResult> LogoutAsync(int userId, string refreshToken);
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ServiceResult> VerifyOtpAsync(VerifyOtpRequest request);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request);
    Task<UserProfileDto?> GetUserProfileAsync(int userId);
    Task<List<EffectivePermissionDto>> GetEffectivePermissionsAsync(int userId);
}