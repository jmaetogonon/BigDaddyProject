using BigDaddyProject.Domain.Entities.Identity;
using System.Security.Claims;

namespace BigDaddyProject.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}