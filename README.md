# Quantora Backend — Simple Architecture

Fresh backend baseline for Quantora using a simple layered architecture.

## Request flow

React → Controller → Service → Repository / External Client → Database / Upstox

## Projects

- Quantora.Api — HTTP, JWT configuration, controllers, Swagger, logging, exception handling.
- Quantora.Application — business services, DTOs, contracts/interfaces, exceptions.
- Quantora.Domain — entities.
- Quantora.Infrastructure — Dapper/PostgreSQL, JWT implementation, Upstox client.

## Intentionally not included yet

CQRS, MediatR, AutoMapper, Hangfire, RabbitMQ, and other infrastructure that is not needed by the current features.

## Database

All Quantora tables use PostgreSQL schema `stocks`.

## Security

Secrets should be supplied through .NET User Secrets or environment variables. Never commit database passwords, JWT signing keys, broker secrets, or access tokens.
