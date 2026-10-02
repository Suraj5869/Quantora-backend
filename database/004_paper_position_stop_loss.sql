-- Apply this migration once to the Supabase PostgreSQL database.
ALTER TABLE stocks.paper_positions
    ADD COLUMN IF NOT EXISTS stop_loss_price numeric(18,4) NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'paper_positions_stop_loss_positive'
          AND conrelid = 'stocks.paper_positions'::regclass
    ) THEN
        ALTER TABLE stocks.paper_positions
            ADD CONSTRAINT paper_positions_stop_loss_positive
            CHECK (stop_loss_price IS NULL OR stop_loss_price > 0);
    END IF;
END $$;
