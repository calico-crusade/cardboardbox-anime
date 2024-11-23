
DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1
		FROM pg_type
		WHERE typname = 'discord_attachment'
	) THEN
		CREATE TYPE discord_attachment AS (
			id TEXT,
			filename TEXT,
			url TEXT,
			type TEXT,
			description TEXT
		);
	END IF;
	
	IF NOT EXISTS (
		SELECT 1
		FROM pg_type
		WHERE typname = 'discord_sticker'
	) THEN
		CREATE TYPE discord_sticker AS (
			id TEXT,
			name TEXT,
			description TEXT,
			type INT,
			format INT,
			url TEXT
		);
	END IF;
END$$;

CREATE TABLE IF NOT EXISTS discord_message_logs (
	id BIGSERIAL PRIMARY KEY,

	message_id TEXT NOT NULL,
	author_id TEXT NOT NULL,
	channel_id TEXT NULL,
	guild_id TEXT NULL,
	thread_id TEXT NULL,
	reference_id TEXT NULL,
	send_timestamp TIMESTAMP NOT NULL,
	attachments discord_attachment[] NOT NULL DEFAULT '{}',
	mentioned_channels TEXT[] NOT NULL DEFAULT '{}',
	mentioned_roles TEXT[] NOT NULL DEFAULT '{}',
	mentioned_users TEXT[] NOT NULL DEFAULT '{}',
	stickers discord_sticker[] NOT NULL DEFAULT '{}',
	content TEXT NULL,
	message_type INT NOT NULL,
	message_source INT NOT NULL,

	created_at TIMESTAMP not null default CURRENT_TIMESTAMP,
	updated_at TIMESTAMP not null default CURRENT_TIMESTAMP,
	deleted_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS dml_idx_mid ON discord_message_logs (message_id);
CREATE INDEX IF NOT EXISTS dml_idx_aid ON discord_message_logs (author_id);
CREATE INDEX IF NOT EXISTS dml_idx_gid ON discord_message_logs (guild_id);
CREATE INDEX IF NOT EXISTS dml_idx_cid ON discord_message_logs (channel_id);