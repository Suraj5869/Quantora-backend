using Serilog;

namespace Quantora.Api.Extensions
{
    public static class LoggingExtensions
    {
        public static IHostBuilder UseQuantoraLogging(
            this IHostBuilder hostBuilder)
        {
            hostBuilder.UseSerilog((context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "Quantora")
                    .WriteTo.Console()
                    .WriteTo.File(
                        "logs/quantora-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        shared: true);
            });

            return hostBuilder;
        }
    }
}
