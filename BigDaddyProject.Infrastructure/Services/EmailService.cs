using BigDaddyProject.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BigDaddyProject.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetOtpAsync(string toEmail, string name, string otp)
    {
        var expiryMins = _config["PasswordPolicy:OtpExpiryMinutes"] ?? "15";
        var subject = "BigDaddy – Password Reset OTP";
        var body = $@"
            <div style='font-family:Segoe UI,sans-serif;max-width:500px;margin:0 auto;padding:24px;'>
                <h2 style='color:#1a1a2e;'>Password Reset</h2>
                <p>Hi <strong>{name}</strong>,</p>
                <p>Your one-time password (OTP) to reset your BigDaddy account password is:</p>
                <div style='text-align:center;margin:24px 0;'>
                    <span style='font-size:36px;font-weight:bold;letter-spacing:8px;
                                 color:#1a1a2e;background:#ffc107;padding:12px 24px;
                                 border-radius:8px;'>{otp}</span>
                </div>
                <p style='color:#666;'>This OTP expires in <strong>{expiryMins} minutes</strong>.</p>
                <p style='color:#666;'>If you did not request this, please ignore this email.</p>
            </div>";

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string name, string temporaryPassword)
    {
        var subject = "Welcome to BigDaddy – Your Account is Ready";
        var body = $@"
            <div style='font-family:Segoe UI,sans-serif;max-width:500px;margin:0 auto;padding:24px;'>
                <h2 style='color:#1a1a2e;'>Welcome, {name}!</h2>
                <p>Your BigDaddy account has been created. Here are your login credentials:</p>
                <table style='background:#f8f9fa;padding:16px;border-radius:8px;width:100%;'>
                    <tr><td><strong>Email:</strong></td><td>{toEmail}</td></tr>
                    <tr><td><strong>Password:</strong></td><td>{temporaryPassword}</td></tr>
                </table>
                <p style='color:#e74c3c;'><strong>Please log in and change your password immediately.</strong></p>
            </div>";

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendPasswordChangedNotificationAsync(string toEmail, string name)
    {
        var subject = "BigDaddy – Password Changed";
        var body = $@"
            <div style='font-family:Segoe UI,sans-serif;max-width:500px;margin:0 auto;padding:24px;'>
                <h2 style='color:#1a1a2e;'>Password Changed</h2>
                <p>Hi <strong>{name}</strong>,</p>
                <p>Your BigDaddy account password was changed at {DateTime.UtcNow:f} UTC.</p>
                <p style='color:#e74c3c;'>If you did not make this change, contact your administrator immediately.</p>
            </div>";

        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["EmailSettings:FromName"], _config["EmailSettings:FromEmail"]));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:Host"],
                int.Parse(_config["EmailSettings:Port"] ?? "587"),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _config["EmailSettings:Username"],
                _config["EmailSettings:Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
            throw; // Callers decide whether to swallow or re-throw
        }
    }
}