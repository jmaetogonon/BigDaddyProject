namespace BigDaddyProject.Domain.Entities.Identity;

public class PasswordResetOtp
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string OtpHash { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}