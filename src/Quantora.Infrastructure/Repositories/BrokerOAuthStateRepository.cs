using Dapper;
using Npgsql;
using Quantora.Application.Interfaces;
using Quantora.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Infrastructure.Repositories
{
    public sealed class BrokerOAuthStateRepository
    : IBrokerOAuthStateRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BrokerOAuthStateRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateAsync(
            Guid userId,
            string broker,
            string stateHash,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            INSERT INTO stocks.broker_oauth_states
            (
                id,
                user_id,
                broker,
                state_hash,
                expires_at,
                created_at
            )
            VALUES
            (
                @Id,
                @UserId,
                @Broker,
                @StateHash,
                @ExpiresAt,
                @CreatedAt
            );
            """;

            var now = DateTimeOffset.UtcNow;

            await using var db =
                (NpgsqlConnection)_connectionFactory.CreateConnection();

            await db.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Broker = broker,
                        StateHash = stateHash,
                        ExpiresAt = expiresAt,
                        CreatedAt = now
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<Guid?> ConsumeAsync(
            string stateHash,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            UPDATE stocks.broker_oauth_states
            SET consumed_at = @Now
            WHERE state_hash = @StateHash
              AND consumed_at IS NULL
              AND expires_at > @Now
            RETURNING user_id;
            """;

            var now = DateTimeOffset.UtcNow;

            await using var db =
                (NpgsqlConnection)_connectionFactory.CreateConnection();

            return await db.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        StateHash = stateHash,
                        Now = now
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
