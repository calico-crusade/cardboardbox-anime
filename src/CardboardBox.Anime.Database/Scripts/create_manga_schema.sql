CREATE TABLE manga (
    id BIGSERIAL PRIMARY KEY,

    title text not null,
    source_id text not null,
    provider text not null,
    url text not null,
    cover text not null,
    tags text[] not null default '{}',
    alt_titles text[] not null default '{}',
    description text not null,

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_title_hash UNIQUE(source_id, provider)
);

CREATE TABLE manga_chapter (
    id BIGSERIAL PRIMARY KEY,

    manga_id bigint not null references manga(id),
    title text not null,
    url text not null,
    source_id text not null,
    ordinal numeric not null,
    language text not null,
    pages text[] not null default '{}',

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_chapter UNIQUE(manga_id, source_id, language)
);