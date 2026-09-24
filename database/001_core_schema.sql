CREATE SCHEMA IF NOT EXISTS stocks;

CREATE TABLE IF NOT EXISTS stocks.users
(
    id uuid PRIMARY KEY,
    full_name varchar(150) NOT NULL,
    email varchar(320) NOT NULL,
    password_hash text NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    is_email_verified boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    last_login_at timestamptz NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email_lower
    ON stocks.users (LOWER(email));

CREATE TABLE IF NOT EXISTS stocks.refresh_tokens
(
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES stocks.users(id) ON DELETE CASCADE,
    token_hash varchar(128) NOT NULL,
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL,
    revoked_at timestamptz NULL,
    replaced_by_token_id uuid NULL REFERENCES stocks.refresh_tokens(id),
    created_by_ip varchar(64) NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_refresh_tokens_token_hash
    ON stocks.refresh_tokens(token_hash);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id
    ON stocks.refresh_tokens(user_id);
