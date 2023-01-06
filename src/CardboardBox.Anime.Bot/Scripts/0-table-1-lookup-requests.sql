CREATE TABLE IF NOT EXISTS lookup_requests (
	id INTEGER PRIMARY KEY,
	image_url TEXT NOT NULL,
	message_id TEXT NOT NULL,
	channel_id TEXT NOT NULL,
	guild_id TEXT NOT NULL,
	author_id TEXT NOT NULL,
	response_id TEXT NOT NULL,
	results TEXT,

	created_at TEXT NOT NULL,
	updated_at TEXT NOT NULL,
	deleted_at TEXT
)