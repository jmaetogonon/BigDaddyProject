using System.ComponentModel.DataAnnotations;

namespace BigDaddyProject.Application.DTOs;

//public class AuthDtos
//{
//    //public class LoginRequest
//    //{
//    //    [Required] public string Email { get; set; } = string.Empty;

//    //    [Required]
//    //    public string Password { get; set; } = string.Empty;
//    //}

//    public record LoginResponse(
//        string AccessToken,
//        string RefreshToken,
//        DateTime ExpiresAt,
//        UserProfileDto User
//    );

//    public record RefreshTokenRequest([Required] string RefreshToken);

//    public record ChangePasswordRequest(
//        [Required] string CurrentPassword,
//        [Required][MinLength(8)] string NewPassword,
//        [Required] string ConfirmNewPassword
//    );

//    public record ForgotPasswordRequest([Required][EmailAddress] string Email);

//    public record ResetPasswordRequest(
//        [Required] string Token,
//        [Required][MinLength(8)] string NewPassword,
//        [Required] string ConfirmNewPassword
//    );

//    public record VerifyOtpRequest(
//        [Required] string Email,
//        [Required] string Otp
//    );

//    public record UserProfileDto(
//        int Id,
//        string Name,
//        string Email,
//        string? Mobile,
//        string Status,
//        List<string> Roles,
//        List<EffectivePermissionDto> Permissions
//    );

//    public record EffectivePermissionDto(
//        int PermissionId,
//        string PermissionName,
//        string Group,
//        int AccessLevel // 0=None, 1=Individual, 2=Organization
//    );
//}

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileDto User
);

public record RefreshTokenRequest([Required] string RefreshToken);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][MinLength(8)] string NewPassword,
    [Required] string ConfirmNewPassword
);

public record ForgotPasswordRequest([Required][EmailAddress] string Email);

public record VerifyOtpRequest(
    [Required][EmailAddress] string Email,
    [Required] string Otp
);

public record ResetPasswordRequest(
    [Required] string Token,
    [Required][MinLength(8)] string NewPassword,
    [Required] string ConfirmNewPassword
);

public record UserProfileDto(
    int Id,
    string Name,
    string Email,
    string? Mobile,
    string Status,
    List<string> Roles,
    List<EffectivePermissionDto> Permissions
);

public record EffectivePermissionDto(
    int PermissionId,
    string PermissionName,
    string Group,
    int AccessLevel
);