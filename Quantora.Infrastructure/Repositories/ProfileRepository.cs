using Dapper;
using Quantora.Application.Modules.Profile.Interfaces;
using Quantora.Domain.Entities;
using Quantora.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Infrastructure.Repositories
{
    public sealed class ProfileRepository : IProfileRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProfileRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<User?> GetByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            SELECT
                id AS Id,
                full_name AS FullName,
                email AS Email,
                password_hash AS PasswordHash,
                is_active AS IsActive,
                is_email_verified AS IsEmailVerified,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                last_login_at AS LastLoginAt
            FROM stocks.users
            WHERE id = @UserId;
            """;

            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> UpdateFullNameAsync(
            Guid userId,
            string fullName,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            UPDATE stocks.users
            SET
                full_name = @FullName,
                updated_at = @UpdatedAt
            WHERE id = @UserId;
            """;

            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            var affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId,
                        FullName = fullName,
                        UpdatedAt = updatedAt
                    },
                    cancellationToken: cancellationToken));

            return affectedRows > 0;
        }
    }
}
