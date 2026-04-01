using BigDaddyProject.Domain.Enums;
using BigDaddyProject.Web.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BigDaddyProject.Web.Extensions;

public static class PermissionAuthorizationExtensions
{
    // All permission names from the spec
    private static readonly string[] AllPermissions =
    [
        // Organization
        "Project Data Administrator", "Manage Transaction", "Manage Interest",
        "Manage Notification", "Manage Audit Logs", "Manage Other Selling Entity",
        "View Status Other", "View Summary Other",

        // General
        "Access Project", "Edit Project Set",

        // Pricing
        "View Price 1 Available Unit", "View Price 2 Available Unit",
        "View Price 3 Available Unit", "View Price 4 Available Unit",
        "View Price 5 Available Unit", "View Price 6 Available Unit",
        "View Price 7 Available Unit", "View Price 8 Available Unit",
        "View Price 9 Available Unit", "View Price 10 Available Unit",
        "View Price Sold Unit",

        // Status
        "View Status", "View Status - Pending Reservation", "View Status - Reserved",
        "View Status - SPA Signed", "View Status - SPA Stamped",
        "Change Status - Not Released/Available", "App Sales Record",

        // Interest
        "View Interest", "Submit Interest", "Edit Interest",

        // Booking
        "Mark Pending Reserve", "Mark Reserved", "Mark Sold",
        "Mark SPA Signed", "Mark SPA Stamped", "Set Not Release",

        // Booking Edit
        "Edit Pending Reservation", "Edit Reserved", "Edit Sold",
        "Edit SPA Signed", "Edit SPA Stamp", "Edit Unit Price",
        "Edit Sales Rep", "Select Other Selling Entity",
        "Edit Reserved Date", "Edit Sold Date", "Edit SPA Signed Date",
        "Edit SPA Stamped Date", "Change Default Deposit Amount", "Booking Reissue Date",

        // Booking Cancellation
        "Cancel Pending Reserve", "Cancel Reserved", "Cancel Sold",
        "Cancel SPA Signed/Stamped", "Confirm Cancel", "Edit Appointment",
        "Arrived", "Cancel Appointment", "Delete Appointment",

        // Booking Document
        "Generate/Upload Document", "View Sensitive Document",
        "Generate/Upload Sensitive Document",
        "Senior Management AML Overriding for OTP",
        "Check Verified AML", "SOLD Trading Editor",

        // Announcement
        "Receive Sold/Available Notification", "Send Announcement",
        "Receive Status Change to SPA Signed Email",
        "Receive Status Change to SPA Stamped Email",
        "Receive Reserved Notification", "No View Agency",

        // Reports
        "View Summary Report", "Email Reports", "View Detail Summary Report",

        // CMS
        "CMS Access", "CMS - Unit Tab Access", "CMS - Permission Access",
        "CMS - Transaction Import/Export", "CMS - Unit Interest Import",
        "CMS - Unit Interest Export", "CMS - Unit Interest - Show Duplicate Data",
        "CMS - Edit Price",

        // Data Access
        "View Sensitive Files",

        // Architect
        "Confirm Progressive Status", "Mark Notify",
        "Mark Payment Receive", "Deferment Request"
    ];

    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPermissionPolicies();

        return services;
    }

    private static AuthorizationBuilder AddPermissionPolicies(this AuthorizationBuilder builder)
    {
        foreach (var permission in AllPermissions)
        {
            foreach (var level in Enum.GetValues<AccessLevel>())
            {
                if (level == AccessLevel.None) continue;

                var policyName = HasPermissionAttribute.BuildPolicyName(permission, level);
                builder.AddPolicy(policyName, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission, level)));
            }
        }

        return builder;
    }
}