using BigDaddyProject.Application.Common;
using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Entities.Identity;
using BigDaddyProject.Domain.Enums;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BigDaddyProject.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IEmailService email,
        IAuditService audit,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _db = db;
        _email = email;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ServiceResult<UserDetailDto>> CreateUserAsync(
        CreateUserRequest request, int createdByUserId)
    {
        if (await _userManager.FindByEmailAsync(request.Email) != null)
            return ServiceResult<UserDetailDto>.Fail("Email is already in use.");

        var password = string.IsNullOrWhiteSpace(request.InitialPassword)
            ? GenerateRandomPassword()
            : request.InitialPassword;

        var user = new ApplicationUser
        {
            UserName = request.Email,
            NormalizedUserName = request.Email.ToUpper(),
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpper(),
            Name = request.Name,
            NRICName = request.NRICName,
            CEANumber = request.CEANumber,
            CEAExpiry = request.CEAExpiry,
            Mobile = request.Mobile,
            Gender = request.Gender,
            Status = request.Status,
            MustChangePassword = request.MustChangePassword,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return ServiceResult<UserDetailDto>.Fail(
                string.Join(", ", result.Errors.Select(e => e.Description)));

        // Assign teams
        foreach (var teamId in request.TeamIds.Distinct())
            _db.AgentTeams.Add(new AgentTeam { UserId = user.Id, TeamId = teamId });

        // Assign roles
        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await _db.Roles.FindAsync(roleId);
            if (role?.Name != null)
                await _userManager.AddToRoleAsync(user, role.Name);
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(createdByUserId, AuditOperation.UserCreated,
            $"Created user {user.Email}");

        try { await _email.SendWelcomeEmailAsync(user.Email!, user.Name, password); }
        catch (Exception ex) { _logger.LogWarning(ex, "Welcome email failed for {Email}", user.Email); }

        return ServiceResult<UserDetailDto>.Ok(await BuildDetailDtoAsync(user));
    }

    public async Task<ServiceResult<UserDetailDto>> UpdateUserAsync(
        int userId, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ServiceResult<UserDetailDto>.Fail("User not found.");

        user.Name = request.Name;
        user.NRICName = request.NRICName;
        user.CEANumber = request.CEANumber;
        user.CEAExpiry = request.CEAExpiry;
        user.Mobile = request.Mobile;
        user.Gender = request.Gender;
        user.Status = request.Status;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.NormalizedEmail = request.Email.ToUpper();
        user.NormalizedUserName = request.Email.ToUpper();
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        // Replace team assignments
        var existingTeams = await _db.AgentTeams.Where(at => at.UserId == userId).ToListAsync();
        _db.AgentTeams.RemoveRange(existingTeams);
        foreach (var teamId in request.TeamIds.Distinct())
            _db.AgentTeams.Add(new AgentTeam { UserId = userId, TeamId = teamId });

        // Replace role assignments
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await _db.Roles.FindAsync(roleId);
            if (role?.Name != null)
                await _userManager.AddToRoleAsync(user, role.Name);
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, AuditOperation.UserUpdated, $"User {user.Email} updated");

        return ServiceResult<UserDetailDto>.Ok(await BuildDetailDtoAsync(user));
    }

    public async Task<ServiceResult> SetStatusAsync(int userId, string status)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ServiceResult.Fail("User not found.");

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var op = status == "Active" ? AuditOperation.UserActivated : AuditOperation.UserDeactivated;
        await _audit.LogAsync(userId, op, $"User status set to {status}");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AdminResetPasswordAsync(
        int userId, AdminResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ServiceResult.Fail("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return ServiceResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = request.MustChangePassword;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _audit.LogAsync(userId, AuditOperation.PasswordResetCompleted,
            "Admin reset user password");

        return ServiceResult.Ok();
    }

    public async Task<PagedResult<UserListDto>> GetUsersAsync(
        int page, int pageSize, string? search, string? status)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.Name.Contains(search) || (u.Email != null && u.Email.Contains(search)));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => u.Status == status);

        var total = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.AgentTeams).ThenInclude(at => at.Team)
            .ToListAsync();

        var items = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListDto(
                user.Id, user.Name, user.Email!, user.Mobile, user.Status,
                user.AgentTeams.Select(at => at.Team.Name).ToList(),
                roles.ToList(),
                user.CreatedAt));
        }

        return new PagedResult<UserListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.AgentTeams).ThenInclude(at => at.Team)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user == null ? null : await BuildDetailDtoAsync(user);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<UserDetailDto> BuildDetailDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var roleDetails = await _db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => new RoleSummaryDto(r.Id, r.Name!, r.ProjectId))
            .ToListAsync();

        // Ensure navigation loaded
        if (!user.AgentTeams.Any())
        {
            await _db.Entry(user)
                .Collection(u => u.AgentTeams)
                .Query()
                .Include(at => at.Team)
                .LoadAsync();
        }

        return new UserDetailDto(
            user.Id, user.Name, user.NRICName, user.CEANumber, user.CEAExpiry,
            user.Mobile, user.Email!, user.Gender, user.Photo, user.Status,
            user.MustChangePassword, user.CreatedAt, user.UpdatedAt,
            user.AgentTeams.Select(at => new TeamSummaryDto(
                at.Team.Id, at.Team.ProjectId, at.Team.Name)).ToList(),
            roleDetails);
    }

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

        var chars = new char[12];
        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];

        for (int i = 4; i < 12; i++)
            chars[i] = all[bytes[i] % all.Length];

        return new string(chars.OrderBy(_ => Guid.NewGuid()).ToArray());
    }
}
