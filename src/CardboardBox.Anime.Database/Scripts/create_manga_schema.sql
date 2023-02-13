CREATE TYPE manga_attribute AS (
    name text,
    value text
);

CREATE TABLE manga (
    id BIGSERIAL PRIMARY KEY,

    title text not null,
    source_id text not null,
    provider text not null,
    hash_id text not null,
    url text not null,
    cover text not null,
    tags text[] not null default '{}',
    alt_titles text[] not null default '{}',
    description text not null,
    nsfw boolean not null default False,
    referer text,
    source_created timestamp,

    attributes manga_attribute[] not null default '{}',

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_title_hash UNIQUE(source_id, provider)
);

CREATE OR REPLACE FUNCTION f_ciarr2text(text[]) RETURNS text LANGUAGE sql IMMUTABLE AS $$SELECT array_to_string($1, ',')$$;

ALTER TABLE manga
    ADD COLUMN fts tsvector
        GENERATED ALWAYS AS (
            to_tsvector('english',
                title || ' ' || description || ' ' || f_ciarr2text(alt_titles)
            )
        ) STORED;

CREATE TABLE manga_chapter (
    id BIGSERIAL PRIMARY KEY,

    manga_id bigint not null references manga(id),
    title text not null,
    url text not null,
    source_id text not null,
    ordinal numeric not null,
    volume numeric,
    language text not null,
    pages text[] not null default '{}',
    external_url text,
    attributes manga_attribute[] not null default '{}',

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_chapter UNIQUE(manga_id, source_id, language)
);

CREATE TABLE manga_progress (
    id BIGSERIAL PRIMARY KEY,
    
    profile_id bigint not null references profiles(id),
    manga_id bigint not null references manga(id),
    manga_chapter_id bigint not null references manga_chapter(id),
    page_index int not null,

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_progress UNIQUE(profile_id, manga_id)
);

CREATE TABLE manga_bookmarks (
    id BIGSERIAL PRIMARY KEY,

    profile_id bigint not null references profiles(id),
    manga_id bigint not null references manga(id),
    manga_chapter_id bigint not null references manga_chapter(id),
    pages int[] not null default '{}',

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_bookmarks UNIQUE(profile_id, manga_id, manga_chapter_id)
);

CREATE TABLE manga_favourites (
    id BIGSERIAL PRIMARY KEY,

    profile_id bigint not null references profiles(id),
    manga_id bigint not null references manga(id),

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_favourites UNIQUE(profile_id, manga_id)
);



