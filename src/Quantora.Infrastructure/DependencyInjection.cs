using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quantora.Application.Interfaces;
using Quantora.Infrastructure.Authentication;
using Quantora.Infrastructure.Broker.Upstox;
using Quantora.Infrastructure.Persistence;
using Quantora.Infrastructure.Repositories;
using Quantora.Infrastructure.Security;

namespace Quantora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IBrokerProvider, UpstoxBrokerProvider>();
        services.AddHttpClient<IUpstoxClient, UpstoxClient>();
        services.AddHttpClient<IUpstoxMarketDataClient, UpstoxMarketDataClient>();

        services.AddScoped<IBrokerConnectionRepository, BrokerConnectionRepository>();
        services.AddScoped<Quantora.Application.Services.IPaperTradingRepository, PaperTradingRepository>();
        services.AddScoped<IBrokerOAuthStateRepository, BrokerOAuthStateRepository>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();
        return services;
    }
}
