using BigDaddyProject.Domain.Entities.Identity;
using BigDaddyProject.Infrastructure;
using BigDaddyProject.Infrastructure.Data;
using BigDaddyProject.Infrastructure.Data.Seed;
using BigDaddyProject.Web.Components;
using BigDaddyProject.Web.Extensions;
using BigDaddyProject.Web.Middleware;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bigdaddy-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services
    .AddInfrastructure(builder.Configuration)       // DB + Identity + App Services
    .AddJwtAuthentication(builder.Configuration)     // JWT Bearer
    .AddCorsPolicy(builder.Configuration)            // CORS for mobile
    .AddSwaggerDocs()                                // Swagger / OpenAPI
    .AddBlazorServices()                             // Blazor SSR + Controllers
    .AddPermissionAuthorization();                   // Custom HasPermission policies

// ─── Build App ────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Seed Database ────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    await DatabaseSeeder.SeedAsync(db, userManager, roleManager, logger);
}


// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseExceptionHandling();         // 1. Global exception handler — always first

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BigDaddy API v1"));
}
else
{
    app.UseHsts();
}


//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("MobileAppPolicy");
app.UseAuthentication();            // 2. Read + validate JWT
app.UseAuthorization();             // 3. Evaluate [Authorize] / [HasPermission]
app.UseTokenValidation();           // 4. Extra: check user still Active in DB
app.UseAntiforgery();

// ─── Endpoints ────────────────────────────────────────────────────────────────
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
