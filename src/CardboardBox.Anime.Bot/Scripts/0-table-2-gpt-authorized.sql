CREATE TABLE IF NOT EXISTS gpt_authorized (
	id INTEGER PRIMARY KEY,
	
	user_id TEXT NOT NULL DEFAULT '0',
	server_id TEXT NOT NULL DEFAULT '0',	
	type TEXT NOT NULL,

	created_at TEXT NOT NULL DEFAULT current_timestamp,
	updated_at TEXT NOT NULL,
	deleted_at TEXT,
	
	UNIQUE (user_id, server_id)
)