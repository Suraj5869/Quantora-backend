CREATE TABLE IF NOT EXISTS stocks.news_articles (
    id UUID PRIMARY KEY,
    source_name TEXT NOT NULL,
    title TEXT NOT NULL,
    summary TEXT NULL,
    url TEXT NOT NULL,
    image_url TEXT NULL,
    published_at TIMESTAMPTZ NOT NULL,
    fetched_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    category TEXT NOT NULL DEFAULT 'Market',
    sentiment TEXT NOT NULL DEFAULT 'Neutral',
    sentiment_score NUMERIC(6,4) NOT NULL DEFAULT 0,
    impact TEXT NOT NULL DEFAULT 'Low',
    relevance_score NUMERIC(6,4) NOT NULL DEFAULT 0,
    tickers TEXT[] NOT NULL DEFAULT '{}',
    content_hash TEXT NOT NULL UNIQUE
);
CREATE INDEX IF NOT EXISTS ix_news_published_at ON stocks.news_articles(published_at DESC);
CREATE INDEX IF NOT EXISTS ix_news_category ON stocks.news_articles(category);
CREATE INDEX IF NOT EXISTS ix_news_sentiment ON stocks.news_articles(sentiment);
CREATE INDEX IF NOT EXISTS ix_news_tickers ON stocks.news_articles USING GIN(tickers);
