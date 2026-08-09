using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Quantora.Api.Configurations;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddQuantoraSwagger(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Quantora API",
                    Version = "v1",
                    Description = "Quantora AI-powered trading platform API"
                });

            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT access token."
                });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", document),
                        new List<string>()
                    }
                });
        });

        return services;
    }
}