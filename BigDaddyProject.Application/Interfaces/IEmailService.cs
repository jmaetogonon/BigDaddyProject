namespace BigDaddyProject.Application.Interfaces;


public interface IEmailService
{
    Task SendPasswordResetOtpAsync(string toEmail, string name, string otp);
    Task SendWelcomeEmailAsync(string toEmail, string name, string temporaryPassword);
    Task SendPasswordChangedNotificationAsync(string toEmail, string name);
}