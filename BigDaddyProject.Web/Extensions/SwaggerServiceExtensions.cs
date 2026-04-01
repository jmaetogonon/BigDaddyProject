using Microsoft.OpenApi;

namespace BigDaddyProject.Web.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BigDaddy API",
                Version = "v1",
                Description = "Authentication & Authorization API for BigDaddyProject Mobile App"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization. Enter: Bearer {your_token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                BearerFormat = "JWT", //
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(document =>
     new OpenApiSecurityRequirement
     {
         [new OpenApiSecuritySchemeReference("Bearer", document)] = []
     });
        });

        return services;
    }
}
