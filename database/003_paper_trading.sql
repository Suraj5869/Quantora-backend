CREATE TABLE IF NOT EXISTS stocks.paper_accounts (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL UNIQUE REFERENCES stocks.users(id) ON DELETE CASCADE,
    initial_cash numeric(18,2) NOT NULL DEFAULT 100000.00 CHECK (initial_cash > 0),
    available_cash numeric(18,2) NOT NULL DEFAULT 100000.00 CHECK (available_cash >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS stocks.paper_positions (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL REFERENCES stocks.paper_accounts(id) ON DELETE CASCADE,
    instrument_key varchar(120) NOT NULL,
    trading_symbol varchar(40) NOT NULL,
    quantity numeric(18,4) NOT NULL CHECK (quantity > 0),
    average_price numeric(18,4) NOT NULL CHECK (average_price >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, instrument_key)
);
CREATE TABLE IF NOT EXISTS stocks.paper_orders (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL REFERENCES stocks.paper_accounts(id) ON DELETE CASCADE,
    instrument_key varchar(120) NOT NULL,
    trading_symbol varchar(40) NOT NULL,
    side varchar(4) NOT NULL CHECK (side IN ('BUY','SELL')),
    quantity numeric(18,4) NOT NULL CHECK (quantity > 0),
    order_type varchar(10) NOT NULL DEFAULT 'MARKET' CHECK (order_type = 'MARKET'),
    status varchar(20) NOT NULL CHECK (status IN ('FILLED','REJECTED')),
    execution_price numeric(18,4) NULL,
    total_value numeric(18,2) NULL,
    realized_pnl numeric(18,2) NOT NULL DEFAULT 0,
    rejection_reason varchar(300) NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    executed_at timestamptz NULL
);
CREATE INDEX IF NOT EXISTS ix_paper_orders_account_created ON stocks.paper_orders(account_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_paper_positions_account ON stocks.paper_positions(account_id);
