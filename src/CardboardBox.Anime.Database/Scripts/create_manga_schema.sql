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

    ordinal_index int not null default 0,

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
    max_chapter_row_num numeric not null,
    latest_chapter timestamp not null,

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

CREATE OR REPLACE VIEW manga_avg_dif
AS
WITH numbered_chapters AS (
    SELECT
        m.id as manga_id,
        c.id as chapter_id,
        c.volume,
        c.ordinal,
        c.created_at as chapter_created,
        ROW_NUMBER() OVER (
            PARTITION BY c.manga_id, c.volume, c.ordinal
            ORDER BY c.created_at DESC
        ) as chapter_row
    FROM manga m
    JOIN manga_chapter c ON m.id = c.manga_id
    WHERE
        date_trunc('day', m.created_at) <> date_trunc('day', c.created_at) AND
        m.deleted_at IS NULL AND
        c.deleted_at IS NULL
), time_between AS (
    SELECT
        (MAX(chapter_created) - MIN(chapter_created)) / nullif(COUNT(*) - 1, 0) as average,
        manga_id
    FROM numbered_chapters
    WHERE chapter_row = 1
    GROUP BY manga_id
)
SELECT *
FROM time_between
WHERE average IS NOT NULL;

CREATE MATERIALIZED VIEW IF NOT EXISTS manga_chapter_grouped AS
SELECT
    MAX(ordinal) as last_chapter,
    MIN(ordinal) as first_chapter,
    MAX(ordinal_index) as row_num,
    COUNT(DISTINCT ordinal) as unique_chapter_count,
    COUNT(ordinal) as chapter_count,
    MAX(created_at) as latest_chapter,
    manga_id
FROM manga_chapter
WHERE
    deleted_at IS NULL
GROUP BY manga_id
ORDER BY manga_id;

CREATE OR REPLACE VIEW manga_stats AS 
SELECT
    m.id as manga_id,
    (
        SELECT c.id
        FROM manga_chapter c
        WHERE
            c.manga_id = m.id AND
            c.ordinal = mg.last_chapter AND
            c.deleted_at IS NULL
        ORDER BY (CASE WHEN c.external_url IS NULL THEN 0 ELSE 1 END), c.created_at DESC
        LIMIT 1
    ) as last_chapter_id,
    mg.last_chapter as last_chapter_ordinal,
    (
        SELECT c.id
        FROM manga_chapter c
        WHERE
            c.manga_id = m.id AND
            c.ordinal = mg.first_chapter AND
            c.deleted_at IS NULL
        ORDER BY (CASE WHEN c.external_url IS NULL THEN 0 ELSE 1 END), c.created_at ASC
        LIMIT 1
    ) as first_chapter_id,
    mg.first_chapter as first_chapter_ordinal,
    mg.chapter_count as chapter_count,
    mg.unique_chapter_count as unique_chapter_count,
    mg.row_num as max_chapter_row_num,
    mg.latest_chapter as latest_chapter
FROM manga m
JOIN manga_chapter_grouped mg ON mg.manga_id = m.id
WHERE m.deleted_at IS NULL;

CREATE OR REPLACE VIEW manga_progress_ext AS
WITH touched AS (
    SELECT
        DISTINCT x.*
    FROM (
        SELECT manga_id, profile_id FROM manga_favourites
        UNION
        SELECT manga_id, profile_id FROM manga_bookmarks
        UNION
        SELECT manga_id, profile_id FROM manga_progress
    ) x
    JOIN manga m ON m.id = x.manga_id
    WHERE m.deleted_at IS NULL
), progress AS (
    SELECT
        mp.*,
        mmc.max_chapter_row_num AS max_chapter_num,
        mc.ordinal_index AS chapter_num,
        COALESCE(ARRAY_LENGTH(mc.pages, 1), 0) AS page_count,
        (
            CASE
                WHEN mmc.first_chapter_id = mc.id AND mp.page_index IS NULL THEN 0
                WHEN mc.id = mmc.last_chapter_id THEN 100
                ELSE LEAST(ROUND(mc.ordinal_index / CAST(mmc.max_chapter_row_num AS DECIMAL) * 100, 2), 100)
            END
        ) AS chapter_progress,
        LEAST((COALESCE(ROUND((mp.page_index + 1) / CAST(ARRAY_LENGTH(mc.pages, 1) AS DECIMAL), 2), 0) * 100), 100) AS page_progress,
        row_number() OVER (
            PARTITION BY mp.id
            ORDER BY mp.id ASC
        ) AS row_number
    FROM manga_progress mp
    JOIN manga_stats mmc ON mmc.manga_id = mp.manga_id
    JOIN manga_chapter mc ON mc.id = mp.manga_chapter_id
    WHERE
        mp.deleted_at IS NULL AND
        mc.deleted_at IS NULL
), bookmark_count AS (
    SELECT
        manga_id,
        profile_id,
        SUM(ARRAY_LENGTH(pages, 1)) AS count
    FROM manga_bookmarks
    GROUP BY manga_id, profile_id
)
SELECT
    t.*,
    COALESCE(p.manga_chapter_id, s.first_chapter_id) AS manga_chapter_id,
    s.first_chapter_id AS first_chapter_id,
    p.manga_chapter_id AS progress_chapter_id,
    p.id AS progress_id,
    s.last_chapter_ordinal AS max_chapter_ordinal,
    p.chapter_num AS chapter_num,
    COALESCE(p.page_count, 0) AS page_count,
    COALESCE(p.page_index, 0) AS page_index,
    COALESCE(p.chapter_progress, 0) AS chapter_progress,
    COALESCE(p.page_progress, 0) AS page_progress,
    f.id IS NOT NULL AS favourite,
    COALESCE(b.pages, '{}') AS bookmarks,
    COALESCE(bc.count, 0) AS bookmark_count,
    bc.count IS NOT NULL AS has_bookmarks,
    COALESCE(p.chapter_progress, 0) >= 100 AS completed,
    p.manga_chapter_id IS NOT NULL AS in_progress
FROM touched t
JOIN manga_stats s ON s.manga_id = t.manga_id
LEFT JOIN bookmark_count bc ON bc.manga_id = t.manga_id AND bc.profile_id = t.profile_id
LEFT JOIN progress p ON
    p.manga_id = t.manga_id AND
    p.profile_id = t.profile_id AND
    p.deleted_at IS NULL AND
    p.row_number = 1
LEFT JOIN manga_favourites f ON
    f.manga_id = t.manga_id AND
    f.profile_id = t.profile_id AND
    f.deleted_at IS NULL
LEFT JOIN manga_bookmarks b ON
    b.manga_id = t.manga_id AND
    b.profile_id = t.profile_id AND
    b.manga_chapter_id = p.manga_chapter_id AND
    b.deleted_at IS NULL
ORDER BY t.profile_id ASC, t.manga_id DESC;