CREATE EXTENSION pg_trgm;

CREATE TYPE manga_attribute AS (
    name text,
    value text
);

CREATE TYPE manga_chapter_progress AS (
    chapter_id BIGINT,
    page_index INT
);

CREATE TABLE IF NOT EXISTS manga (
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
    uploader bigint REFERENCES profiles(id),
    display_title TEXT,
    ordinal_volume_reset BOOLEAN NOT NULL DEFAULT FALSE,

    attributes manga_attribute[] not null default '{}',

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_title_hash UNIQUE(source_id, provider)
);

CREATE OR REPLACE FUNCTION f_ciarr2text(text[]) RETURNS text LANGUAGE sql IMMUTABLE AS $$SELECT array_to_string($1, ',')$$;

ALTER TABLE manga
    ADD COLUMN IF NOT EXISTS 
        fts tsvector
        GENERATED ALWAYS AS (
            to_tsvector('english',
                title || ' ' || description || ' ' || f_ciarr2text(alt_titles)
            )
        ) STORED;

ALTER TABLE manga
    ADD COLUMN IF NOT EXISTS
        uploader bigint REFERENCES profiles(id);

ALTER TABLE manga 
    ADD COLUMN IF NOT EXISTS 
        display_title TEXT;

CREATE TABLE IF NOT EXISTS manga_chapter (
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

CREATE TABLE IF NOT EXISTS manga_progress (
    id BIGSERIAL PRIMARY KEY,
    
    profile_id bigint not null references profiles(id),
    manga_id bigint not null references manga(id),
    manga_chapter_id bigint references manga_chapter(id),
    page_index int,
    read manga_chapter_progress[] not null default '{}',

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_progress UNIQUE(profile_id, manga_id)
);

CREATE TABLE IF NOT EXISTS manga_bookmarks (
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

CREATE TABLE IF NOT EXISTS manga_favourites (
    id BIGSERIAL PRIMARY KEY,

    profile_id bigint not null references profiles(id),
    manga_id bigint not null references manga(id),

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_manga_favourites UNIQUE(profile_id, manga_id)
);

CREATE OR REPLACE VIEW manga_attributes
AS
    SELECT
        DISTINCT
        id,
        nsfw,
        (unnest(attributes)).name as name,
        (unnest(attributes)).value as value
    FROM manga;

CREATE TABLE IF NOT EXISTS manga_stats (
    manga_id bigint not null references manga(id),
    last_chapter_id bigint not null references manga_chapter(id),
    last_chapter_ordinal numeric not null,
    first_chapter_id bigint not null references manga_chapter(id),
    first_chapter_ordinal numeric not null,
    chapter_count numeric not null,
    unique_chapter_count numeric not null,

    CONSTRAINT uiq_manga_stats UNIQUE(manga_id)
);

CREATE TABLE IF NOT EXISTS manga_progress_ext (
    manga_id BIGINT NOT NULL REFERENCES manga(id),
    profile_id BIGINT NOT NULL REFERENCES profiles(id),
    manga_chapter_id BIGINT NOT NULL REFERENCES manga_chapter(id),
    first_chapter_id BIGINT NOT NULL REFERENCES manga_chapter(id),
    progress_chapter_id BIGINT REFERENCES manga_chapter(id),
    progress_id BIGINT REFERENCES manga_progress(id),
    max_chapter_ordinal NUMERIC NOT NULL,
    chapter_num NUMERIC,
    page_count NUMERIC NOT NULL,
    page_index NUMERIC NOT NULL,
    chapter_progress NUMERIC NOT NULL,
    page_progress NUMERIC NOT NULL,
    favourite BOOLEAN NOT NULL DEFAULT FALSE,
    bookmarks INT[] NOT NULL DEFAULT '{}',
    bookmark_count NUMERIC NOT NULL,
    has_bookmarks BOOLEAN NOT NULL DEFAULT FALSE,
    completed BOOLEAN NOT NULL DEFAULT FALSE,
    in_progress BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT uiq_manga_progress_ext UNIQUE(manga_id, profile_id)
);

CREATE INDEX manga_idx_description ON manga USING gist (description gist_trgm_ops);

CREATE MATERIALIZED VIEW IF NOT EXISTS manga_similar_tags AS
WITH manga_tags AS (
    SELECT
        id as manga_id,
        UNNEST(tags) as tag
    FROM manga
)
SELECT
    a.manga_id as first_manga_id,
    b.manga_id as second_manga_id,
    COUNT(*) as same_tags
FROM manga_tags a
JOIN manga_tags b ON
    a.manga_id > b.manga_id AND
    a.tag = b.tag
GROUP BY a.manga_id, b.manga_id;