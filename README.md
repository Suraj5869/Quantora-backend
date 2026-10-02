# Quantora Backend — Simple Architecture

Quantora uses a simple layered architecture:

React → Controller → Service → Repository / External Client → Database / Upstox

## Projects

- `Quantora.Api` — HTTP API, JWT authentication, Swagger, logging and exception handling.
- `Quantora.Application` — business services, DTOs and interfaces.
- `Quantora.Domain` — domain entities.
- `Quantora.Infrastructure` — Dapper/PostgreSQL, authentication, broker and market-data clients.

CQRS/MediatR is intentionally not used.

## Database

All Quantora tables use PostgreSQL schema `stocks`. Apply the SQL files in `database/` in numeric order to a fresh database:

1. `001_core_schema.sql`
2. `002_broker_connections.sql`

## Local configuration

Secrets must not be committed. The tracked `appsettings.json` deliberately contains empty secret placeholders. From `src/Quantora.Api`, configure .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<YOUR_ROTATED_SUPABASE_CONNECTION_STRING>"
dotnet user-secrets set "Jwt:SecretKey" "<A_RANDOM_SECRET_AT_LEAST_32_CHARACTERS>"
dotnet user-secrets set "Security:EncryptionKey" "<BASE64_ENCODED_32_BYTE_KEY>"
dotnet user-secrets set "Upstox:ClientId" "<YOUR_UPSTOX_CLIENT_ID>"
dotnet user-secrets set "Upstox:ClientSecret" "<YOUR_UPSTOX_CLIENT_SECRET>"
```

Generate an AES-256 encryption key with PowerShell:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Use the resulting Base64 value for `Security:EncryptionKey`. Use a newly rotated Supabase database password in the connection string. Because the previous password was committed to Git, removing it from the latest file does not remove it from Git history.

For deployed environments, use environment variables or your hosting provider's secret manager instead of User Secrets.

## Market data API

The authenticated endpoints use the current user's encrypted Upstox OAuth token:

- `GET /api/v1/market-data/intraday?instrumentKey=NSE_EQ%7CINE848E01016&unit=minutes&interval=1`
- `GET /api/v1/market-data/candles?instrumentKey=NSE_EQ%7CINE848E01016&unit=minutes&interval=1&fromDate=2026-09-01&toDate=2026-09-10`

Historical candles are provided by Upstox Candle V3. Intraday data represents the current trading day's candles; it is not a live streaming feed. Access requires an active broker connection with a valid access token.
