using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Quantora.Infrastructure.Persistence;

public sealed class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured.");
    }

    public IDbConnection CreateConnection() =>
        new NpgsqlConnection(_connectionString);
}
