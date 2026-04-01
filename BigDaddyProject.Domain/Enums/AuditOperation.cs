namespace BigDaddyProject.Domain.Enums;

public enum AuditOperation
{
    Login,
    LoginFailed,
    Logout,
    PasswordChanged,
    PasswordResetRequested,
    PasswordResetCompleted,
    UserCreated,
    UserUpdated,
    UserActivated,
    UserDeactivated,
    TeamAssignmentChanged,
    RoleAssignmentChanged,
    PermissionAssignmentChanged
}