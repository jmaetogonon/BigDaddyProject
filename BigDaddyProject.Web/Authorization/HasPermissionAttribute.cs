using BigDaddyProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace BigDaddyProject.Web.Authorization;

/// <summary>
/// Requires the user to have a specific permission at or above the minimum access level.
/// </summary>
/// <example>
/// [HasPermission("Mark Reserved")]
/// [HasPermission("View Summary Report", AccessLevel.Organization)]
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(
        string permissionName,
        AccessLevel minimumLevel = AccessLevel.Individual)
        : base(BuildPolicyName(permissionName, minimumLevel))
    {
    }

    internal static string BuildPolicyName(string permissionName, AccessLevel level)
        => $"Permission:{permissionName}:{(int)level}";
}