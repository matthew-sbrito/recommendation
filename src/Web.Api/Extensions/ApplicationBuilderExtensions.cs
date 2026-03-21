using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;

namespace Web.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseScalarWithOpenApi(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/docs", options =>
        {
            options.WithTitle("Recommendation API")
                .AddPreferredSecuritySchemes("BearerAuth");
        });

        return app;
    }
}
