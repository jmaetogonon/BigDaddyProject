using BigDaddyProject.Domain.Enums;

namespace BigDaddyProject.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(int userId, AuditOperation operation, string details, string? ipAddress = null);
}