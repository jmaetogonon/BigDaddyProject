namespace BigDaddyProject.Web.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();

    public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder app)
        => app.UseMiddleware<TokenValidationMiddleware>();
}
