using Dapper;
using Npgsql;
using Quantora.Application.Interfaces;
using Quantora.Domain.Entities;
using Quantora.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Infrastructure.Repositories
{
    public sealed class BrokerConnectionRepository : IBrokerConnectionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BrokerConnectionRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<BrokerConnection?> GetAsync(
            Guid userId,
            string broker,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                broker AS Broker,
                broker_user_id AS BrokerUserId,
                access_token_encrypted AS AccessTokenEncrypted,
                extended_token_encrypted AS ExtendedTokenEncrypted,
                is_active AS IsActive,
                connected_at AS ConnectedAt,
                updated_at AS UpdatedAt
            FROM stocks.broker_connections
            WHERE user_id = @UserId
              AND broker = @Broker;
            """;

            await using var connection =
                (NpgsqlConnection)_connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<BrokerConnection>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId,
                        Broker = broker
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task UpsertAsync(
            BrokerConnection connection,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            INSERT INTO stocks.broker_connections
            (
                id,
                user_id,
                broker,
                broker_user_id,
                access_token_encrypted,
                extended_token_encrypted,
                is_active,
                connected_at,
                updated_at
            )
            VALUES
            (
                @Id,
                @UserId,
                @Broker,
                @BrokerUserId,
                @AccessTokenEncrypted,
                @ExtendedTokenEncrypted,
                @IsActive,
                @ConnectedAt,
                @UpdatedAt
            )
            ON CONFLICT (user_id, broker)
            DO UPDATE SET
                broker_user_id = EXCLUDED.broker_user_id,
                access_token_encrypted = EXCLUDED.access_token_encrypted,
                extended_token_encrypted = EXCLUDED.extended_token_encrypted,
                is_active = EXCLUDED.is_active,
                updated_at = EXCLUDED.updated_at;
            """;

            await using var db =
                (NpgsqlConnection)_connectionFactory.CreateConnection();

            await db.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    connection,
                    cancellationToken: cancellationToken));
        }

        public async Task DisconnectAsync(
    Guid userId,
    string broker,
    CancellationToken cancellationToken = default)
        {
            const string sql = """
        UPDATE stocks.broker_connections
        SET
            is_active = false,
            updated_at = @UpdatedAt
        WHERE user_id = @UserId
          AND broker = @Broker;
        """;

            await using var db =
                (NpgsqlConnection)_connectionFactory.CreateConnection();

            await db.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId,
                        Broker = broker,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
