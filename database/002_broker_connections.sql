CREATE SCHEMA IF NOT EXISTS stocks;

CREATE TABLE IF NOT EXISTS stocks.broker_connections
(
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES stocks.users(id) ON DELETE CASCADE,
    broker varchar(50) NOT NULL,
    broker_user_id varchar(150) NOT NULL,
    access_token_encrypted text NOT NULL,
    extended_token_encrypted text NULL,
    is_active boolean NOT NULL DEFAULT true,
    connected_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    CONSTRAINT uq_broker_connections_user_broker UNIQUE (user_id, broker)
);

CREATE INDEX IF NOT EXISTS ix_broker_connections_user_active
    ON stocks.broker_connections(user_id, is_active);

CREATE TABLE IF NOT EXISTS stocks.broker_oauth_states
(
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES stocks.users(id) ON DELETE CASCADE,
    broker varchar(50) NOT NULL,
    state_hash varchar(128) NOT NULL UNIQUE,
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL,
    consumed_at timestamptz NULL
);

CREATE INDEX IF NOT EXISTS ix_broker_oauth_states_expiry
    ON stocks.broker_oauth_states(expires_at);

CREATE INDEX IF NOT EXISTS ix_broker_oauth_states_user
    ON stocks.broker_oauth_states(user_id);
