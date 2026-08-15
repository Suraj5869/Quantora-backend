using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Application.Modules.Profile.Interfaces;
using Quantora.Infrastructure.Authentication;
using Quantora.Infrastructure.Persistence;
using Quantora.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IProfileRepository, ProfileRepository>();
            return services;
        }
    }
}
