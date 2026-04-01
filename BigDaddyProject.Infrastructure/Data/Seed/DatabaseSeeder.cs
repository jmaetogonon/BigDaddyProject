using BigDaddyProject.Domain.Entities.Identity;
using BigDaddyProject.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BigDaddyProject.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger? logger = null)
    {
        await context.Database.MigrateAsync();
        logger?.LogInformation("Database migrated successfully.");

        // 1. Seed permissions first
        await SeedPermissionsAsync(context, logger);

        // 2. Seed identity roles
        var sysAdminRole = await EnsureRoleAsync(roleManager, "SystemAdministrator", "SYSTEM");
        await EnsureRoleAsync(roleManager, "Manager", "SYSTEM");
        await EnsureRoleAsync(roleManager, "EndUser", "SYSTEM");

        // 3. Assign ALL permissions (Organization level) to SystemAdministrator role
        await AssignAllPermissionsToRoleAsync(context, sysAdminRole, logger);

        // 4. Create default SystemAdmin team
        var adminTeam = await EnsureTeamAsync(context, "SYSTEM", "System Administrators");

        // 5. Link the SystemAdministrator role to the admin team via TeamRole
        await EnsureTeamRoleAsync(context, adminTeam.Id, sysAdminRole.Id);

        // 6. Seed the admin user
        var adminUser = await EnsureAdminUserAsync(userManager, logger);

        // 7. Assign admin user to the admin team (this is what makes permissions work!)
        await EnsureAgentTeamAsync(context, adminUser.Id, adminTeam.Id);

        await context.SaveChangesAsync();
        logger?.LogInformation("Database seeding completed.");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static async Task<ApplicationRole> EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager, string name, string projectId)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role == null)
        {
            role = new ApplicationRole { Name = name, NormalizedName = name.ToUpper(), ProjectId = projectId };
            await roleManager.CreateAsync(role);
        }
        return role;
    }

    private static async Task<Team> EnsureTeamAsync(
        ApplicationDbContext context, string projectId, string name)
    {
        var team = await context.Teams
            .FirstOrDefaultAsync(t => t.ProjectId == projectId && t.Name == name);

        if (team == null)
        {
            team = new Team { ProjectId = projectId, Name = name };
            context.Teams.Add(team);
            await context.SaveChangesAsync();
        }
        return team;
    }

    private static async Task EnsureTeamRoleAsync(
        ApplicationDbContext context, int teamId, int roleId)
    {
        var exists = await context.TeamRoles
            .AnyAsync(tr => tr.TeamId == teamId && tr.RoleId == roleId);

        if (!exists)
        {
            context.TeamRoles.Add(new TeamRole { TeamId = teamId, RoleId = roleId });
            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureAdminUserAsync(
        UserManager<ApplicationUser> userManager, ILogger? logger)
    {
        const string adminEmail = "admin@bigdaddy.com";
        var user = await userManager.FindByEmailAsync(adminEmail);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                NormalizedUserName = adminEmail.ToUpper(),
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpper(),
                Name = "System Administrator",
                Status = "Active",
                EmailConfirmed = true,
                MustChangePassword = false
            };

            var result = await userManager.CreateAsync(user, "Admin@123456!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "SystemAdministrator");
                logger?.LogInformation("Admin user created: {Email}", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger?.LogError("Failed to create admin user: {Errors}", errors);
            }
        }
        return user;
    }

    private static async Task EnsureAgentTeamAsync(
        ApplicationDbContext context, int userId, int teamId)
    {
        var exists = await context.AgentTeams
            .AnyAsync(at => at.UserId == userId && at.TeamId == teamId);

        if (!exists)
        {
            context.AgentTeams.Add(new AgentTeam { UserId = userId, TeamId = teamId });
            await context.SaveChangesAsync();
        }
    }

    private static async Task AssignAllPermissionsToRoleAsync(
        ApplicationDbContext context, ApplicationRole role, ILogger? logger)
    {
        var allPermissions = await context.Permissions.ToListAsync();
        if (!allPermissions.Any()) return;

        var existingRolePermissionIds = await context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var toAdd = allPermissions
            .Where(p => !existingRolePermissionIds.Contains(p.Id))
            .Select(p => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = p.Id,
                AccessLevel = AccessLevel.Organization // SystemAdmin gets full access
            })
            .ToList();

        if (toAdd.Any())
        {
            await context.RolePermissions.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
            logger?.LogInformation(
                "Assigned {Count} permissions to role {Role}", toAdd.Count, role.Name);
        }
    }

    private static async Task SeedPermissionsAsync(
        ApplicationDbContext context, ILogger? logger)
    {
        if (await context.Permissions.AnyAsync()) return;

        var permissions = new List<Permission>
        {
            // Organization Level
            new() { Name = "Project Data Administrator",    Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 1 },
            new() { Name = "Manage Transaction",            Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 2 },
            new() { Name = "Manage Interest",               Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 3 },
            new() { Name = "Manage Notification",           Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 4 },
            new() { Name = "Manage Audit Logs",             Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 5 },
            new() { Name = "Manage Other Selling Entity",   Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 6 },
            new() { Name = "View Status Other",             Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 7 },
            new() { Name = "View Summary Other",            Type = PermissionType.Organization, Group = "Organization", DisplayOrder = 8 },

            // Property - General
            new() { Name = "Access Project",  Type = PermissionType.Property, Group = "General", DisplayOrder = 10 },
            new() { Name = "Edit Project Set",Type = PermissionType.Property, Group = "General", DisplayOrder = 11 },

            // Property - Pricing
            new() { Name = "View Price 1 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 20 },
            new() { Name = "View Price 2 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 21 },
            new() { Name = "View Price 3 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 22 },
            new() { Name = "View Price 4 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 23 },
            new() { Name = "View Price 5 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 24 },
            new() { Name = "View Price 6 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 25 },
            new() { Name = "View Price 7 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 26 },
            new() { Name = "View Price 8 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 27 },
            new() { Name = "View Price 9 Available Unit", Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 28 },
            new() { Name = "View Price 10 Available Unit",Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 29 },
            new() { Name = "View Price Sold Unit",        Type = PermissionType.Property, Group = "Pricing", DisplayOrder = 30 },

            // Property - Status
            new() { Name = "View Status",                         Type = PermissionType.Property, Group = "Status", DisplayOrder = 40 },
            new() { Name = "View Status - Pending Reservation",   Type = PermissionType.Property, Group = "Status", DisplayOrder = 41 },
            new() { Name = "View Status - Reserved",              Type = PermissionType.Property, Group = "Status", DisplayOrder = 42 },
            new() { Name = "View Status - SPA Signed",            Type = PermissionType.Property, Group = "Status", DisplayOrder = 43 },
            new() { Name = "View Status - SPA Stamped",           Type = PermissionType.Property, Group = "Status", DisplayOrder = 44 },
            new() { Name = "Change Status - Not Released/Available", Type = PermissionType.Property, Group = "Status", DisplayOrder = 45 },
            new() { Name = "App Sales Record",                    Type = PermissionType.Property, Group = "Status", DisplayOrder = 46 },

            // Property - Interest
            new() { Name = "View Interest",   Type = PermissionType.Property, Group = "Interest", DisplayOrder = 50 },
            new() { Name = "Submit Interest", Type = PermissionType.Property, Group = "Interest", DisplayOrder = 51 },
            new() { Name = "Edit Interest",   Type = PermissionType.Property, Group = "Interest", DisplayOrder = 52 },

            // Property - Booking
            new() { Name = "Mark Pending Reserve", Type = PermissionType.Property, Group = "Booking", DisplayOrder = 60 },
            new() { Name = "Mark Reserved",        Type = PermissionType.Property, Group = "Booking", DisplayOrder = 61 },
            new() { Name = "Mark Sold",            Type = PermissionType.Property, Group = "Booking", DisplayOrder = 62 },
            new() { Name = "Mark SPA Signed",      Type = PermissionType.Property, Group = "Booking", DisplayOrder = 63 },
            new() { Name = "Mark SPA Stamped",     Type = PermissionType.Property, Group = "Booking", DisplayOrder = 64 },
            new() { Name = "Set Not Release",      Type = PermissionType.Property, Group = "Booking", DisplayOrder = 65 },

            // Property - Booking Edit
            new() { Name = "Edit Pending Reservation",    Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 70 },
            new() { Name = "Edit Reserved",               Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 71 },
            new() { Name = "Edit Sold",                   Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 72 },
            new() { Name = "Edit SPA Signed",             Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 73 },
            new() { Name = "Edit SPA Stamp",              Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 74 },
            new() { Name = "Edit Unit Price",             Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 75 },
            new() { Name = "Edit Sales Rep",              Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 76 },
            new() { Name = "Select Other Selling Entity", Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 77 },
            new() { Name = "Edit Reserved Date",          Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 78 },
            new() { Name = "Edit Sold Date",              Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 79 },
            new() { Name = "Edit SPA Signed Date",        Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 80 },
            new() { Name = "Edit SPA Stamped Date",       Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 81 },
            new() { Name = "Change Default Deposit Amount",Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 82 },
            new() { Name = "Booking Reissue Date",        Type = PermissionType.Property, Group = "Booking-Edit", DisplayOrder = 83 },

            // Property - Booking Cancellation
            new() { Name = "Cancel Pending Reserve",    Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 90 },
            new() { Name = "Cancel Reserved",           Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 91 },
            new() { Name = "Cancel Sold",               Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 92 },
            new() { Name = "Cancel SPA Signed/Stamped", Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 93 },
            new() { Name = "Confirm Cancel",            Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 94 },
            new() { Name = "Edit Appointment",          Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 95 },
            new() { Name = "Arrived",                   Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 96 },
            new() { Name = "Cancel Appointment",        Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 97 },
            new() { Name = "Delete Appointment",        Type = PermissionType.Property, Group = "Booking-Cancellation", DisplayOrder = 98 },

            // Property - Booking Documents
            new() { Name = "Generate/Upload Document",                  Type = PermissionType.Property, Group = "Booking-Document", DisplayOrder = 100 },
            new() { Name = "View Sensitive Document",                   Type = PermissionType.Property, Group = "Booking-Document", DisplayOrder = 101 },
            new() { Name = "Generate/Upload Sensitive Document",        Type = PermissionType.Property, Group = "Booking-Document", DisplayOrder = 102 },
            new() { Name = "Senior Management AML Overriding for OTP",  Type = PermissionType.Property, Group = "Booking-Document", DisplayOrder = 103 },
            new() { Name = "Check Verified AML",                        Type = PermissionType.Property, Group = "Booking-Document", DisplayOrder = 104 },
            new() { Name = "SOLD Trading Editor",                       Type = PermissionType.Property, Group = "Booking-Document", DisplayOrder = 105 },

            // Property - Announcements
            new() { Name = "Receive Sold/Available Notification",        Type = PermissionType.Property, Group = "Announcement", DisplayOrder = 110 },
            new() { Name = "Send Announcement",                          Type = PermissionType.Property, Group = "Announcement", DisplayOrder = 111 },
            new() { Name = "Receive Status Change to SPA Signed Email",  Type = PermissionType.Property, Group = "Announcement", DisplayOrder = 112 },
            new() { Name = "Receive Status Change to SPA Stamped Email", Type = PermissionType.Property, Group = "Announcement", DisplayOrder = 113 },
            new() { Name = "Receive Reserved Notification",              Type = PermissionType.Property, Group = "Announcement", DisplayOrder = 114 },
            new() { Name = "No View Agency",                             Type = PermissionType.Property, Group = "Announcement", DisplayOrder = 115 },

            // Property - Reports
            new() { Name = "View Summary Report",        Type = PermissionType.Property, Group = "Reports", DisplayOrder = 120 },
            new() { Name = "Email Reports",              Type = PermissionType.Property, Group = "Reports", DisplayOrder = 121 },
            new() { Name = "View Detail Summary Report", Type = PermissionType.Property, Group = "Reports", DisplayOrder = 122 },

            // Property - CMS
            new() { Name = "CMS Access",                              Type = PermissionType.Property, Group = "CMS", DisplayOrder = 130 },
            new() { Name = "CMS - Unit Tab Access",                   Type = PermissionType.Property, Group = "CMS", DisplayOrder = 131 },
            new() { Name = "CMS - Permission Access",                 Type = PermissionType.Property, Group = "CMS", DisplayOrder = 132 },
            new() { Name = "CMS - Transaction Import/Export",         Type = PermissionType.Property, Group = "CMS", DisplayOrder = 133 },
            new() { Name = "CMS - Unit Interest Import",              Type = PermissionType.Property, Group = "CMS", DisplayOrder = 134 },
            new() { Name = "CMS - Unit Interest Export",              Type = PermissionType.Property, Group = "CMS", DisplayOrder = 135 },
            new() { Name = "CMS - Unit Interest - Show Duplicate Data",Type = PermissionType.Property, Group = "CMS", DisplayOrder = 136 },
            new() { Name = "CMS - Edit Price",                        Type = PermissionType.Property, Group = "CMS", DisplayOrder = 137 },

            // Property - Data Access
            new() { Name = "View Sensitive Files", Type = PermissionType.Property, Group = "DataAccess", DisplayOrder = 140 },

            // Property - Architect/Lawyer
            new() { Name = "Confirm Progressive Status", Type = PermissionType.Property, Group = "Architect", DisplayOrder = 150 },
            new() { Name = "Mark Notify",                Type = PermissionType.Property, Group = "Architect", DisplayOrder = 151 },
            new() { Name = "Mark Payment Receive",       Type = PermissionType.Property, Group = "Architect", DisplayOrder = 152 },
            new() { Name = "Deferment Request",          Type = PermissionType.Property, Group = "Architect", DisplayOrder = 153 },
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
        logger?.LogInformation("Seeded {Count} permissions.", permissions.Count);
    }
}
