using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;

namespace Web.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseScalarUi(this WebApplication app)
    {
        app.MapScalarApiReference("/api/docs", options =>
        {
            options
                .WithTitle("OverTicketing API")
                .WithTheme(ScalarTheme.Default)
                .WithOperationTitleSource(OperationTitleSource.Summary)
                .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme)
                .EnablePersistentAuthentication();
        });

        return app;
    }
}
