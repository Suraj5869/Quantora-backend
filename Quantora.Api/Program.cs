using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Quantora.Api.Configurations;
using Quantora.Api.Extensions;
using Quantora.Api.Middlewares;
using Quantora.Api.Services;
using Quantora.Application;
using Quantora.Application.Common.Interfaces;
using Quantora.Application.Configurations;
using Quantora.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseQuantoraLogging();

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection(
        ApplicationSettings.SectionName));

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(
        builder.Configuration.GetSection(
            JwtSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(settings.SecretKey),
        "JWT SecretKey is required.")
    .Validate(
        settings =>
            settings.SecretKey.Length >= 32,
        "JWT SecretKey must contain at least 32 characters.")
    .ValidateOnStart();

var jwtSettings =
    builder.Configuration
        .GetSection(JwtSettings.SectionName)
        .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.SecretKey))
            };
    });

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("QuantoraFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddQuantoraSwagger();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("QuantoraFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
