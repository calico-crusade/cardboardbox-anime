-- CREATE manga TABLE
CREATE TABLE manga (
    id BIGSERIAL PRIMARY KEY,
	
    title text not null,
    source_id text not null,
    url text not null,
    cover text not null,
    provider text not null,

    tags text[] not null default '{}',
	
    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_provider_source UNIQUE(provider, source_id)
);

-- CREATE manga_chapter TABLE
CREATE TABLE manga_chapter (
    id BIGSERIAL PRIMARY KEY,
	
    manga_id bigint not null,
    source_id text not null,
    title text not null,
    url text not null,
    ordinal decimal not null,

    pages text[] not null default '{}',
	
    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_chapter_ids UNIQUE(manga_id, source_id)
);

-- CREATE manga_state TABLE
CREATE TABLE manga_state (
    id BIGSERIAL PRIMARY KEY,
	
    message_id text not null,
    user_id text not null,
    guild_id text,
    channel_id text,
    source text not null,
    manga_id bigint not null,
    chapter_id bigint,
    page_index int,
    	
    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_state_message_id UNIQUE(message_id)
);

