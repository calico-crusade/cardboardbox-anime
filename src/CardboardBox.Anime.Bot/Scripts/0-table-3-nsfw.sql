CREATE TABLE IF NOT EXISTS nsfw_config (
	id INTEGER PRIMARY KEY,
	guild_id TEXT NOT NULL,
	enabled INTEGER NOT NULL DEFAULT 0,
	ignore_nsfw_channels INTEGER NOT NULL DEFAULT 1,
	ignore_channels TEXT NOT NULL DEFAULT '[]',
	allowed_roles TEXT NOT NULL DEFAULT '[]',
	admin_roles TEXT NOT NULL DEFAULT '[]',
	log_channel_id TEXT,
	classify_hentai INTEGER NOT NULL DEFAULT 60,
	classify_sexy INTEGER NOT NULL DEFAULT 0,
	classify_porn INTEGER NOT NULL DEFAULT 60,
	delete_message INTEGER NOT NULL DEFAULT 1,
	kick_after INTEGER NOT NULL DEFAULT 3,
	ban_after INTEGER NOT NULL DEFAULT 5,

	created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
	deleted_at TEXT,

	UNIQUE (guild_id)
)