using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BigDaddyProject.Web.Middleware;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    private static readonly string[] PublicPaths =
    [
        "/login", "/logout", "/forgot-password", "/reset-password", "/error",
        "/api/auth/login", "/api/auth/refresh", "/api/auth/forgot-password",
        "/api/auth/verify-otp", "/api/auth/reset-password", "/api/auth/web-logout",
        "/_blazor", "/_framework", "/favicon", "/app.css", "/swagger"
    ];

    public TokenValidationMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Let public paths and static files through
        if (IsPublic(path))
        {
            await _next(context);
            return;
        }

        var token = GetToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            var principal = ValidateJwt(token);

            if (principal != null)
            {
                var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(userIdStr, out var userId))
                {
                    var user = await db.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.Status })
                        .FirstOrDefaultAsync();

                    if (user?.Status == "Active")
                    {
                        await _next(context);
                        return;
                    }
                }
            }
        }

        // Not authenticated
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "Unauthorized." });
        }
        else
        {
            ClearCookies(context);
            context.Response.Redirect("/login");
        }
    }

    private static bool IsPublic(string path)
    {
        if (path.Contains('.') && !path.EndsWith(".razor"))
            return true; // static files

        return PublicPaths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetToken(HttpContext context)
    {
        // Cookie first (Blazor SSR), then Authorization header (API clients)
        var cookie = context.Request.Cookies["bdp_access_token"];
        if (!string.IsNullOrEmpty(cookie)) return cookie;

        var auth = context.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return auth["Bearer ".Length..].Trim();

        return null;
    }

    private ClaimsPrincipal? ValidateJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config["JwtSettings:Secret"]!)),
                ValidateIssuer = true,
                ValidIssuer = _config["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }

    private static void ClearCookies(HttpContext context)
    {
        context.Response.Cookies.Delete("bdp_access_token", new CookieOptions { Path = "/" });
        context.Response.Cookies.Delete("bdp_refresh_token", new CookieOptions { Path = "/" });
    }
}