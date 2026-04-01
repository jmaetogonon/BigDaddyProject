using BigDaddyProject.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser, ApplicationRole, int,
    IdentityUserClaim<int>, IdentityUserRole<int>, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<AgentTeam> AgentTeams => Set<AgentTeam>();
    public DbSet<TeamRole> TeamRoles => Set<TeamRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserAuditLog> UserAuditLogs => Set<UserAuditLog>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables to cleaner names
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        // AgentTeam — composite PK
        builder.Entity<AgentTeam>()
            .HasKey(at => new { at.UserId, at.TeamId });

        builder.Entity<AgentTeam>()
            .HasOne(at => at.User)
            .WithMany(u => u.AgentTeams)
            .HasForeignKey(at => at.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AgentTeam>()
            .HasOne(at => at.Team)
            .WithMany(t => t.AgentTeams)
            .HasForeignKey(at => at.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // TeamRole — composite PK
        builder.Entity<TeamRole>()
            .HasKey(tr => new { tr.TeamId, tr.RoleId });

        builder.Entity<TeamRole>()
            .HasOne(tr => tr.Team)
            .WithMany(t => t.TeamRoles)
            .HasForeignKey(tr => tr.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamRole>()
            .HasOne(tr => tr.Role)
            .WithMany(r => r.TeamRoles)
            .HasForeignKey(tr => tr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // RolePermission — composite PK
        builder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store enum as int
        builder.Entity<RolePermission>()
            .Property(rp => rp.AccessLevel)
            .HasConversion<int>();

        // Store AuditOperation enum as int
        builder.Entity<UserAuditLog>()
            .Property(l => l.Operation)
            .HasConversion<int>();

        // Unique index: team name per project
        builder.Entity<Team>()
            .HasIndex(t => new { t.ProjectId, t.Name })
            .IsUnique();

        // Unique index: role name per project
        builder.Entity<ApplicationRole>()
            .HasIndex(r => new { r.ProjectId, r.Name })
            .IsUnique();

        // Index for refresh token lookup
        builder.Entity<UserRefreshToken>()
            .HasIndex(t => t.Token)
            .IsUnique();
    }
}