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
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseQuantoraLogging();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.SecretKey),
        "Jwt:SecretKey is required.")
    .Validate(
        settings => settings.SecretKey.Length >= 32,
        "Jwt:SecretKey must contain at least 32 characters.")
    .ValidateOnStart();

builder.Services
    .AddOptions<UpstoxSettings>()
    .Bind(builder.Configuration.GetSection(UpstoxSettings.SectionName));

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
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "QuantoraFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddQuantoraSwagger();

builder.Services
    .AddOptions<SecuritySettings>()
    .Bind(builder.Configuration.GetSection(
        SecuritySettings.SectionName))
    .Validate(
        settings => !string.IsNullOrWhiteSpace(
            settings.EncryptionKey),
        "Security:EncryptionKey is required.")
    .ValidateOnStart();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("QuantoraFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
