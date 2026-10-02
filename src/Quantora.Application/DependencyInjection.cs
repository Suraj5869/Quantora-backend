using Microsoft.Extensions.DependencyInjection;

namespace Quantora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<Services.IAuthService, Services.AuthService>();
        services.AddScoped<Services.IProfileService, Services.ProfileService>();
        services.AddScoped<Services.IBrokerService, Services.BrokerService>();
        services.AddScoped<Services.IMarketDataService, Services.MarketDataService>();
        services.AddScoped<Services.ITechnicalAnalysisService, Services.TechnicalAnalysisService>();

        return services;
    }
}
