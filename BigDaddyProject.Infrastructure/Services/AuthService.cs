using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Entities.Identity;
using BigDaddyProject.Domain.Enums;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BigDaddyProject.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IJwtService jwt,
        IEmailService email,
        IAuditService audit,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwt;
        _email = email;
        _audit = audit;
        _config = config;
        _logger = logger;
    }

    // ─── Login ────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            // Don't reveal whether user exists
            return ServiceResult<LoginResponse>.Fail("Invalid email or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _audit.LogAsync(user.Id, AuditOperation.LoginFailed, "Invalid password", ipAddress);
            await _userManager.AccessFailedAsync(user); // Increments lockout counter
            return ServiceResult<LoginResponse>.Fail("Invalid email or password.");
        }

        if (user.Status != "Active")
        {
            await _audit.LogAsync(user.Id, AuditOperation.LoginFailed,
                $"Login attempt on {user.Status} account", ipAddress);
            return ServiceResult<LoginResponse>.Fail("Your account is not active. Contact your administrator.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _audit.LogAsync(user.Id, AuditOperation.LoginFailed, "Account locked out", ipAddress);
            return ServiceResult<LoginResponse>.Fail("Your account is temporarily locked. Please try again later.");
        }

        // Reset failed access count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        _db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedByIp = ipAddress
        });
        await _db.SaveChangesAsync();

        var permissions = await GetEffectivePermissionsAsync(user.Id);
        var profile = MapToProfile(user, roles, permissions);

        await _audit.LogAsync(user.Id, AuditOperation.Login, "Login successful", ipAddress);

        var expiryMinutes = int.Parse(_config["JwtSettings:ExpiryMinutes"] ?? "60");
        return ServiceResult<LoginResponse>.Ok(new LoginResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(expiryMinutes),
            profile));
    }

    // ─── Logout ───────────────────────────────────────────────────────────────
    public async Task<ServiceResult> LogoutAsync(int userId, string refreshToken)
    {
        var token = await _db.UserRefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == refreshToken);

        if (token != null)
        {
            token.IsRevoked = true;
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(userId, AuditOperation.Logout, "User logged out");
        return ServiceResult.Ok();
    }

    // ─── Refresh Token ────────────────────────────────────────────────────────
    public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var stored = await _db.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
            return ServiceResult<LoginResponse>.Fail("Invalid or expired refresh token.");

        if (stored.User.Status != "Active")
            return ServiceResult<LoginResponse>.Fail("Account is no longer active.");

        // Rotate: revoke old, issue new
        stored.IsRevoked = true;

        var newRefreshToken = _jwt.GenerateRefreshToken();
        var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");

        _db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = stored.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedByIp = ipAddress
        });

        await _db.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(stored.User);
        var accessToken = _jwt.GenerateAccessToken(stored.User, roles);
        var permissions = await GetEffectivePermissionsAsync(stored.UserId);
        var profile = MapToProfile(stored.User, roles, permissions);
        var expiryMinutes = int.Parse(_config["JwtSettings:ExpiryMinutes"] ?? "60");

        return ServiceResult<LoginResponse>.Ok(new LoginResponse(
            accessToken, newRefreshToken,
            DateTime.UtcNow.AddMinutes(expiryMinutes),
            profile));
    }

    // ─── Change Password ──────────────────────────────────────────────────────
    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ServiceResult.Fail("New password and confirmation do not match.");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ServiceResult.Fail("User not found.");

        var result = await _userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
            return ServiceResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _audit.LogAsync(userId, AuditOperation.PasswordChanged, "Password changed by user");

        try { await _email.SendPasswordChangedNotificationAsync(user.Email!, user.Name); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send password changed email."); }

        return ServiceResult.Ok();
    }

    // ─── Forgot Password ──────────────────────────────────────────────────────
    public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always return OK — don't leak whether email exists
        if (user == null || user.Status != "Active")
            return ServiceResult.Ok();

        // Invalidate existing unused OTPs
        var oldOtps = await _db.PasswordResetOtps
            .Where(o => o.UserId == user.Id && !o.IsUsed)
            .ToListAsync();
        oldOtps.ForEach(o => o.IsUsed = true);

        var rawOtp = GenerateOtp();
        var expiryMins = int.Parse(_config["PasswordPolicy:OtpExpiryMinutes"] ?? "15");
        var resetToken = Guid.NewGuid().ToString("N");

        _db.PasswordResetOtps.Add(new PasswordResetOtp
        {
            UserId = user.Id,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(rawOtp),
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMins)
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Id, AuditOperation.PasswordResetRequested, "OTP requested");

        try { await _email.SendPasswordResetOtpAsync(user.Email!, user.Name, rawOtp); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send OTP email to {Email}", request.Email); }

        return ServiceResult.Ok();
    }

    // ─── Verify OTP ───────────────────────────────────────────────────────────
    public async Task<ServiceResult> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return ServiceResult.Fail("Invalid OTP.");

        var otpRecord = await _db.PasswordResetOtps
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRecord == null || !BCrypt.Net.BCrypt.Verify(request.Otp, otpRecord.OtpHash))
            return ServiceResult.Fail("Invalid or expired OTP.");

        return ServiceResult.Ok();
    }

    // ─── Reset Password ───────────────────────────────────────────────────────
    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ServiceResult.Fail("Passwords do not match.");

        var otpRecord = await _db.PasswordResetOtps
            .Include(o => o.User)
            .FirstOrDefaultAsync(o =>
                o.Token == request.Token && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);

        if (otpRecord == null)
            return ServiceResult.Fail("Invalid or expired reset token.");

        var user = otpRecord.User;
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return ServiceResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));

        otpRecord.IsUsed = true;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.Id, AuditOperation.PasswordResetCompleted, "Password reset");
        return ServiceResult.Ok();
    }

    // ─── Get Profile ──────────────────────────────────────────────────────────
    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetEffectivePermissionsAsync(userId);

        return MapToProfile(user, roles, permissions);
    }

    // ─── Effective Permissions (FIXED) ────────────────────────────────────────
    /// <summary>
    /// Computes effective permissions by collecting roles from TWO sources:
    /// 1. Direct Identity UserRoles (AspNetUserRoles table) — covers seeded admin and
    ///    any user assigned roles directly via UserManager.AddToRoleAsync
    /// 2. Team-inherited roles (AgentTeam → TeamRole) — the primary runtime assignment path
    ///
    /// Then loads all RolePermissions for the combined set of role IDs and
    /// merges by taking the highest AccessLevel per permission.
    /// </summary>
    public async Task<List<EffectivePermissionDto>> GetEffectivePermissionsAsync(int userId)
    {
        // Source 1: Direct Identity role IDs from UserRoles table
        var directRoleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        // Source 2: Team-inherited role IDs
        var teamIds = await _db.AgentTeams
            .Where(at => at.UserId == userId)
            .Select(at => at.TeamId)
            .ToListAsync();

        var teamRoleIds = await _db.TeamRoles
            .Where(tr => teamIds.Contains(tr.TeamId))
            .Select(tr => tr.RoleId)
            .ToListAsync();

        // Union both sources — deduplicated
        var allRoleIds = directRoleIds.Union(teamRoleIds).Distinct().ToList();

        if (!allRoleIds.Any())
            return new List<EffectivePermissionDto>();

        // Load all role permissions for combined role set
        var rolePermissions = await _db.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => allRoleIds.Contains(rp.RoleId))
            .ToListAsync();

        // Merge: highest AccessLevel wins per permission
        var effective = rolePermissions
            .GroupBy(rp => rp.PermissionId)
            .Select(g => new EffectivePermissionDto(
                g.Key,
                g.First().Permission.Name,
                g.First().Permission.Group,
                (int)g.Max(rp => rp.AccessLevel)))
            .Where(ep => ep.AccessLevel > 0) // Exclude None
            .OrderBy(ep => ep.Group)
            .ThenBy(ep => ep.PermissionName)
            .ToList();

        return effective;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private static UserProfileDto MapToProfile(
        ApplicationUser user, IList<string> roles, List<EffectivePermissionDto> permissions)
        => new(
            user.Id, user.Name, user.Email!, user.Mobile,
            user.Status, roles.ToList(), permissions);

    private static string GenerateOtp()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return (Math.Abs(BitConverter.ToInt32(bytes, 0)) % 900000 + 100000).ToString();
    }
}
