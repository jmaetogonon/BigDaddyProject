using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ApplicationDbContext _db;

    public AuthController(IAuthService auth, ApplicationDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    /// <summary>Login — Mobile App and Admin Portal</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.LoginAsync(request, ip);

        if (!result.Success)
            return Unauthorized(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Refresh access token using refresh token</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.RefreshTokenAsync(request.RefreshToken, ip);

        if (!result.Success)
            return Unauthorized(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>Logout — revokes refresh token</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _auth.LogoutAsync(userId, request.RefreshToken);
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Web logout — used by Blazor admin portal sidebar.
    /// Clears cookies server-side and redirects to /login.
    /// This must be a GET so the browser follows the anchor href directly.
    /// </summary>
    [HttpGet("web-logout")]
    [AllowAnonymous]
    public async Task<IActionResult> WebLogout()
    {
        var refreshToken = Request.Cookies["bdp_refresh_token"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var stored = await _db.UserRefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);

            if (stored != null)
            {
                stored.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }

        // Clear auth cookies
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };

        Response.Cookies.Append("bdp_access_token", string.Empty, cookieOptions);
        Response.Cookies.Append("bdp_refresh_token", string.Empty, cookieOptions);

        return Redirect("/login");
    }

    /// <summary>Change password (authenticated user)</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _auth.ChangePasswordAsync(userId, request);

        if (!result.Success)
            return BadRequest(new
            {
                message = result.ErrorMessage,
                errors = result.Errors
            });

        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>
    /// Forgot password — sends OTP to registered email.
    /// Always returns 200 to avoid revealing whether email exists.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _auth.ForgotPasswordAsync(request);
        return Ok(new { message = "If the email exists and is active, an OTP has been sent." });
    }

    /// <summary>Verify OTP before allowing password reset</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _auth.VerifyOtpAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "OTP verified successfully." });
    }

    /// <summary>Reset password using token from OTP email</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _auth.ResetPasswordAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "Password reset successfully. You may now log in." });
    }

    /// <summary>Get currently authenticated user's profile and permissions</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _auth.GetUserProfileAsync(userId);

        if (profile == null)
            return NotFound(new { message = "User not found." });

        return Ok(profile);
    }
}