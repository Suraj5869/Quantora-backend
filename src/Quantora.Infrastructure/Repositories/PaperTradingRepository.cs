using Dapper;
using Npgsql;
using Quantora.Application.DTOs.PaperTrading;
using Quantora.Application.Services;
using Quantora.Infrastructure.Persistence;

namespace Quantora.Infrastructure.Repositories;

public sealed class PaperTradingRepository : IPaperTradingRepository
{
    private const decimal StartingCash = 100000m;
    private readonly IDbConnectionFactory _connectionFactory;
    public PaperTradingRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<PaperAccountDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var account = await EnsureAccountAsync(connection, null, userId, cancellationToken);
        var positions = (await connection.QueryAsync<PaperPositionDto>(new CommandDefinition("""
            SELECT instrument_key AS InstrumentKey, trading_symbol AS TradingSymbol, quantity AS Quantity,
                   average_price AS AveragePrice, average_price AS LastPrice,
                   quantity * average_price AS MarketValue, 0::numeric AS UnrealizedPnl
            FROM stocks.paper_positions WHERE account_id = @AccountId ORDER BY trading_symbol;
            """, new { AccountId = account.Id }, cancellationToken: cancellationToken))).AsList();
        var orders = (await connection.QueryAsync<PaperOrderDto>(new CommandDefinition("""
            SELECT id AS Id, instrument_key AS InstrumentKey, trading_symbol AS TradingSymbol, side AS Side,
                   quantity AS Quantity, status AS Status, execution_price AS ExecutionPrice,
                   total_value AS TotalValue, realized_pnl AS RealizedPnl, rejection_reason AS RejectionReason,
                   created_at AS CreatedAt, executed_at AS ExecutedAt
            FROM stocks.paper_orders WHERE account_id = @AccountId ORDER BY created_at DESC LIMIT 100;
            """, new { AccountId = account.Id }, cancellationToken: cancellationToken))).AsList();
        var invested = positions.Sum(p => p.MarketValue);
        return new PaperAccountDto
        {
            Id = account.Id, InitialCash = account.InitialCash, AvailableCash = account.AvailableCash,
            InvestedValue = invested, PortfolioValue = account.AvailableCash + invested,
            TotalPnl = account.AvailableCash + invested - account.InitialCash,
            Positions = positions, RecentOrders = orders
        };
    }

    public async Task<PaperAccountDto> ResetAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        var account = await EnsureAccountAsync(connection, tx, userId, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM stocks.paper_positions WHERE account_id=@Id;", new { Id = account.Id }, tx, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM stocks.paper_orders WHERE account_id=@Id;", new { Id = account.Id }, tx, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE stocks.paper_accounts SET initial_cash=@Cash, available_cash=@Cash, updated_at=now() WHERE id=@Id;
            """, new { Cash = StartingCash, Id = account.Id }, tx, cancellationToken: cancellationToken));
        await tx.CommitAsync(cancellationToken);
        return await GetAccountAsync(userId, cancellationToken);
    }

    public async Task<PaperOrderDto> PlaceOrderAsync(Guid userId, PlacePaperOrderRequest request, string side, decimal price, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        var account = await EnsureAccountAsync(connection, tx, userId, cancellationToken);
        account = await connection.QuerySingleAsync<AccountRow>(new CommandDefinition("""
            SELECT id AS Id, initial_cash AS InitialCash, available_cash AS AvailableCash
            FROM stocks.paper_accounts WHERE id=@Id FOR UPDATE;
            """, new { Id = account.Id }, tx, cancellationToken: cancellationToken));
        var value = decimal.Round(price * request.Quantity, 2, MidpointRounding.AwayFromZero);
        decimal realizedPnl = 0;
        string? rejection = null;
        var existing = await connection.QuerySingleOrDefaultAsync<PositionRow>(new CommandDefinition("""
            SELECT quantity AS Quantity, average_price AS AveragePrice FROM stocks.paper_positions
            WHERE account_id=@AccountId AND instrument_key=@InstrumentKey FOR UPDATE;
            """, new { AccountId = account.Id, request.InstrumentKey }, tx, cancellationToken: cancellationToken));
        if (side == "BUY" && account.AvailableCash < value) rejection = "Insufficient virtual cash for this order.";
        if (side == "SELL" && (existing is null || existing.Quantity < request.Quantity)) rejection = "Insufficient paper position quantity to sell.";
        var status = rejection is null ? "FILLED" : "REJECTED";
        var id = Guid.NewGuid();
        if (rejection is null)
        {
            if (side == "BUY")
            {
                var newQty = (existing?.Quantity ?? 0m) + request.Quantity;
                var avg = (((existing?.Quantity ?? 0m) * (existing?.AveragePrice ?? 0m)) + price * request.Quantity) / newQty;
                await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO stocks.paper_positions(id, account_id, instrument_key, trading_symbol, quantity, average_price, updated_at)
                    VALUES(@Id,@AccountId,@InstrumentKey,@TradingSymbol,@Quantity,@AveragePrice,now())
                    ON CONFLICT(account_id,instrument_key) DO UPDATE SET quantity=EXCLUDED.quantity,
                      average_price=EXCLUDED.average_price, trading_symbol=EXCLUDED.trading_symbol, updated_at=now();
                    """, new { Id = Guid.NewGuid(), AccountId = account.Id, request.InstrumentKey, request.TradingSymbol, Quantity = newQty, AveragePrice = decimal.Round(avg, 4) }, tx, cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition("UPDATE stocks.paper_accounts SET available_cash=available_cash-@Value, updated_at=now() WHERE id=@Id;", new { Value = value, Id = account.Id }, tx, cancellationToken: cancellationToken));
            }
            else
            {
                realizedPnl = decimal.Round((price - existing!.AveragePrice) * request.Quantity, 2, MidpointRounding.AwayFromZero);
                var remaining = existing.Quantity - request.Quantity;
                if (remaining == 0)
                    await connection.ExecuteAsync(new CommandDefinition("DELETE FROM stocks.paper_positions WHERE account_id=@AccountId AND instrument_key=@InstrumentKey;", new { AccountId = account.Id, request.InstrumentKey }, tx, cancellationToken: cancellationToken));
                else
                    await connection.ExecuteAsync(new CommandDefinition("UPDATE stocks.paper_positions SET quantity=@Quantity, updated_at=now() WHERE account_id=@AccountId AND instrument_key=@InstrumentKey;", new { Quantity = remaining, AccountId = account.Id, request.InstrumentKey }, tx, cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition("UPDATE stocks.paper_accounts SET available_cash=available_cash+@Value, updated_at=now() WHERE id=@Id;", new { Value = value, Id = account.Id }, tx, cancellationToken: cancellationToken));
            }
        }
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO stocks.paper_orders(id,account_id,instrument_key,trading_symbol,side,quantity,order_type,status,
                execution_price,total_value,realized_pnl,rejection_reason,created_at,executed_at)
            VALUES(@Id,@AccountId,@InstrumentKey,@TradingSymbol,@Side,@Quantity,'MARKET',@Status,@Price,@Value,@RealizedPnl,@Rejection,now(),
                CASE WHEN @Status='FILLED' THEN now() ELSE NULL END);
            """, new { Id = id, AccountId = account.Id, request.InstrumentKey, request.TradingSymbol, Side = side, request.Quantity, Status = status, Price = status == "FILLED" ? price : (decimal?)null, Value = status == "FILLED" ? value : (decimal?)null, RealizedPnl = realizedPnl, Rejection = rejection }, tx, cancellationToken: cancellationToken));
        await tx.CommitAsync(cancellationToken);
        return new PaperOrderDto { Id = id, InstrumentKey = request.InstrumentKey, TradingSymbol = request.TradingSymbol,
            Side = side, Quantity = request.Quantity, Status = status, ExecutionPrice = status == "FILLED" ? price : null,
            TotalValue = status == "FILLED" ? value : null, RealizedPnl = realizedPnl, RejectionReason = rejection,
            CreatedAt = DateTimeOffset.UtcNow, ExecutedAt = status == "FILLED" ? DateTimeOffset.UtcNow : null };
    }

    private static async Task<AccountRow> EnsureAccountAsync(NpgsqlConnection connection, NpgsqlTransaction? tx, Guid userId, CancellationToken ct)
    {
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO stocks.paper_accounts(id,user_id,initial_cash,available_cash)
            VALUES(@Id,@UserId,@Cash,@Cash) ON CONFLICT(user_id) DO NOTHING;
            """, new { Id = Guid.NewGuid(), UserId = userId, Cash = StartingCash }, tx, cancellationToken: ct));
        return await connection.QuerySingleAsync<AccountRow>(new CommandDefinition("""
            SELECT id AS Id, initial_cash AS InitialCash, available_cash AS AvailableCash
            FROM stocks.paper_accounts WHERE user_id=@UserId;
            """, new { UserId = userId }, tx, cancellationToken: ct));
    }
    private sealed class AccountRow { public Guid Id { get; init; } public decimal InitialCash { get; init; } public decimal AvailableCash { get; init; } }
    private sealed class PositionRow { public decimal Quantity { get; init; } public decimal AveragePrice { get; init; } }
}
