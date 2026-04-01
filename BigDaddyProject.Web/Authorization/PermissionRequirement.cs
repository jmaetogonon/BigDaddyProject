using BigDaddyProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace BigDaddyProject.Web.Authorization;

/// <summary>
/// Requires the user to have a specific permission at or above the minimum access level.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionName { get; }
    public AccessLevel MinimumLevel { get; }

    public PermissionRequirement(string permissionName, AccessLevel minimumLevel = AccessLevel.Individual)
    {
        PermissionName = permissionName;
        MinimumLevel = minimumLevel;
    }
}