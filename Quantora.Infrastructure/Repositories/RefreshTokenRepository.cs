using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using Npgsql;
using Quantora.Application.Modules.Authentication.DTOs;
using Quantora.Application.Modules.Authentication.Interfaces;
using Quantora.Domain.Entities;
using Quantora.Infrastructure.Persistence;

namespace Quantora.Infrastructure.Repositories
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default
        )
        {
            const string sql = """
                INSERT INTO stocks.refresh_tokens
                (
                    id,
                    user_id,
                    token_hash,
                    expires_at,
                    created_at,
                    revoked_at,
                    replaced_by_token_id,
                    created_by_ip
                )
                VALUES
                (
                    @Id,
                    @UserId,
                    @TokenHash,
                    @ExpiresAt,
                    @CreatedAt,
                    @RevokedAt,
                    @ReplacedByTokenId,
                    @CreatedByIp
                );
                """;

            await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                new CommandDefinition(sql, refreshToken, cancellationToken: cancellationToken)
            );
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default
        )
        {
            const string sql = """
                SELECT
                    id AS Id,
                    user_id AS UserId,
                    token_hash AS TokenHash,
                    expires_at AS ExpiresAt,
                    created_at AS CreatedAt,
                    revoked_at AS RevokedAt,
                    replaced_by_token_id AS ReplacedByTokenId,
                    created_by_ip AS CreatedByIp
                FROM stocks.refresh_tokens
                WHERE token_hash = @TokenHash;
                """;

            await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<RefreshToken>(
                new CommandDefinition(
                    sql,
                    new { TokenHash = tokenHash },
                    cancellationToken: cancellationToken
                )
            );
        }

        public async Task RevokeAsync(
            Guid tokenId,
            Guid? replacedByTokenId = null,
            CancellationToken cancellationToken = default
        )
        {
            const string sql = """
                UPDATE stocks.refresh_tokens
                SET
                    revoked_at = NOW(),
                    replaced_by_token_id = @ReplacedByTokenId
                WHERE id = @TokenId
                  AND revoked_at IS NULL;
                """;

            await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { TokenId = tokenId, ReplacedByTokenId = replacedByTokenId },
                    cancellationToken: cancellationToken
                )
            );
        }

        public async Task<RefreshTokenData?> GetWithUserAsync(
            string tokenHash,
            CancellationToken cancellationToken = default
        )
        {
            const string sql = """
                SELECT
                    rt.id AS TokenId,
                    rt.user_id AS UserId,
                    rt.token_hash AS TokenHash,
                    rt.expires_at AS ExpiresAt,
                    rt.created_at AS CreatedAt,
                    rt.revoked_at AS RevokedAt,
                    rt.replaced_by_token_id AS ReplacedByTokenId,
                    rt.created_by_ip AS CreatedByIp,

                    u.email AS Email,
                    u.full_name AS FullName,
                    u.is_active AS IsActive,
                    u.is_email_verified AS IsEmailVerified

                FROM stocks.refresh_tokens rt

                INNER JOIN stocks.users u
                    ON u.id = rt.user_id

                WHERE rt.token_hash = @TokenHash;
                """;

            await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

            var row = await connection.QuerySingleOrDefaultAsync<RefreshTokenUserRow>(
                new CommandDefinition(
                    sql,
                    new { TokenHash = tokenHash },
                    cancellationToken: cancellationToken
                )
            );

            if (row is null)
            {
                return null;
            }

            return new RefreshTokenData
            {
                Token = new RefreshToken
                {
                    Id = row.TokenId,
                    UserId = row.UserId,
                    TokenHash = row.TokenHash,
                    ExpiresAt = row.ExpiresAt,
                    CreatedAt = row.CreatedAt,
                    RevokedAt = row.RevokedAt,
                    ReplacedByTokenId = row.ReplacedByTokenId,
                    CreatedByIp = row.CreatedByIp,
                },

                UserId = row.UserId,
                Email = row.Email,
                FullName = row.FullName,
                IsActive = row.IsActive,
                IsEmailVerified = row.IsEmailVerified,
            };
        }

        private sealed class RefreshTokenUserRow
        {
            public Guid TokenId { get; init; }
            public Guid UserId { get; init; }

            public string TokenHash { get; init; } = string.Empty;

            public DateTimeOffset ExpiresAt { get; init; }
            public DateTimeOffset CreatedAt { get; init; }

            public DateTimeOffset? RevokedAt { get; init; }

            public Guid? ReplacedByTokenId { get; init; }

            public string? CreatedByIp { get; init; }

            public string Email { get; init; } = string.Empty;

            public string FullName { get; init; } = string.Empty;

            public bool IsActive { get; init; }

            public bool IsEmailVerified { get; init; }
        }
    }
}
