using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BigDaddyProject.Web.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IAuthService _authService;

    public PermissionAuthorizationHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        var permissions = await _authService.GetEffectivePermissionsAsync(userId);

        var match = permissions.FirstOrDefault(p =>
            p.PermissionName.Equals(requirement.PermissionName, StringComparison.OrdinalIgnoreCase));

        if (match != null && (AccessLevel)match.AccessLevel >= requirement.MinimumLevel)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}