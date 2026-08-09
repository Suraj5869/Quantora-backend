using Dapper;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Domain.Entities;
using Quantora.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Infrastructure.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<User?> GetByIdAsync(
            Guid id,
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
            WHERE id = @Id;
            """;

            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(
                    sql,
                    new { Id = id },
                    cancellationToken: cancellationToken));
        }

        public async Task<User?> GetByEmailAsync(
            string email,
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
            WHERE LOWER(email) = LOWER(@Email);
            """;

            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(
                    sql,
                    new { Email = email },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> ExistsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            SELECT EXISTS(
                SELECT 1
                FROM stocksusers
                WHERE LOWER(email) = LOWER(@Email)
            );
            """;

            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    sql,
                    new { Email = email },
                    cancellationToken: cancellationToken));
        }

        public async Task CreateAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            INSERT INTO stocks.users
            (
                id,
                full_name,
                email,
                password_hash,
                is_active,
                is_email_verified,
                created_at,
                updated_at
            )
            VALUES
            (
                @Id,
                @FullName,
                @Email,
                @PasswordHash,
                @IsActive,
                @IsEmailVerified,
                @CreatedAt,
                @UpdatedAt
            );
            """;

            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    user,
                    cancellationToken: cancellationToken));
        }

        public async Task UpdateLastLoginAsync(
            Guid userId,
            DateTimeOffset loginTime,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            UPDATE stocks.users
            SET
                last_login_at = @LoginTime,
                updated_at = NOW()
            WHERE id = @UserId;
            """;

            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId,
                        LoginTime = loginTime
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
