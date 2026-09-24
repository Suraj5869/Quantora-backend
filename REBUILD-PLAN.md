# Quantora backend rebuild

The current repository was used as the reference implementation.

New request path:

Controller -> Service -> Repository / External Client -> Database / Upstox

No CQRS/MediatR is used in this baseline.

Current milestone included:
- Register
- Login
- Refresh token rotation
- Logout
- Current user
- Profile get/update
- Upstox configuration/status
- Upstox authorization URL
- Upstox sandbox place/modify/cancel/get order details

Not included yet:
- OAuth callback
- Durable per-user broker connection storage
- encrypted broker-token storage
- OAuth state persistence/validation
- market data
- watchlist
- portfolio
- paper trading engine

Those will be added after this baseline is running.

IMPORTANT:
The source archive contained a database connection string with an exposed password. Do not copy that value into the new project. Rotate that database credential before continuing if it is still valid.
