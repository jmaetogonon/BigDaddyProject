using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Entities.Identity;
using BigDaddyProject.Domain.Enums;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace BigDaddyProject.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        int userId, AuditOperation operation, string details, string? ipAddress = null)
    {
        try
        {
            _db.UserAuditLogs.Add(new UserAuditLog
            {
                UserId = userId,
                Operation = operation,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Never let audit failure crash the main flow
            _logger.LogError(ex, "Failed to write audit log for user {UserId}, op {Op}",
                userId, operation);
        }
    }
}