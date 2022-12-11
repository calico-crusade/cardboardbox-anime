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

CREATE OR REPLACE function toggle_favourite(platformId text, mangaId bigint) RETURNS integer
AS $$
DECLARE profileId bigint;
BEGIN
    profileId := (
        SELECT
            id
        FROM profiles
        WHERE platform_id = platformId
    );

    IF (profileId IS NULL) THEN RETURN -1; END IF;

    IF EXISTS (
        SELECT 1
        FROM manga_favourites
        WHERE profile_id = profileId AND manga_id = mangaId
    ) THEN
        DELETE FROM manga_favourites WHERE profile_id = profileId AND manga_id = mangaId;
        RETURN 0;
    ELSE
        INSERT INTO manga_favourites (profile_id, manga_id) VALUES (profileId, mangaId);
        RETURN 1;
    END IF;
END
$$
LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_touched_manga(platformId text)
    RETURNS TABLE (
        manga_id BIGINT,
        manga_chapter_id BIGINT,
        first_chapter_id BIGINT,
        progress_chapter_id BIGINT,
        progress_id BIGINT,
        max_chapter_num BIGINT,
        chapter_num BIGINT,
        page_count INT,
        chapter_progress NUMERIC,
        page_progress NUMERIC,
        favourite BIT,
        bookmarks INT[],
        completed BIT
    )
LANGUAGE plpgsql
AS $$
BEGIN
 RETURN QUERY WITH touched_manga AS (
    SELECT DISTINCT m.*, p.id as profile_id FROM manga m
    JOIN manga_bookmarks mb on m.id = mb.manga_id
    JOIN profiles p on mb.profile_id = p.id
    WHERE p.platform_id = platformId

    UNION

    SELECT DISTINCT m.*, p.id as profile_id FROM manga m
    JOIN manga_favourites mb on m.id = mb.manga_id
    JOIN profiles p on mb.profile_id = p.id
    WHERE p.platform_id = platformId

    UNION

    SELECT DISTINCT m.*, p.id as profile_id FROM manga m
    JOIN manga_progress mb on m.id = mb.manga_id
    JOIN profiles p on mb.profile_id = p.id
    WHERE p.platform_id = platformId
), chapter_numbers AS (
    SELECT
        c.*,
        row_number() over (
            PARTITION BY c.manga_id
            ORDER BY c.ordinal ASC
        ) as row_num
    FROM manga_chapter c
    JOIN touched_manga m ON m.id = c.manga_id
), max_chapter_numbers AS (
    SELECT
        c.manga_id,
        MAX(c.row_num) as max,
        MIN(c.id) as first_chapter_id
    FROM chapter_numbers c
    GROUP BY c.manga_id
), progress AS (
    SELECT
        mp.*
    FROM manga_progress mp
    JOIN profiles p ON p.id = mp.profile_id
    WHERE
        p.platform_id = platformId
), records AS (
    SELECT DISTINCT
        m.id as manga_id,
        mc.id as manga_chapter_id,
        mmc.first_chapter_id as first_chapter_id,
        mp.manga_chapter_id as progress_chapter_id,
        mp.id as progress_id,
        mmc.max as max_chapter_num,
        mc.row_num as chapter_num,
        coalesce(array_length(mc.pages, 1), 0) as page_count,
        (
            CASE
                WHEN mmc.first_chapter_id = mc.id AND mp.page_index IS NULL THEN 0
                ELSE round(mc.row_num / CAST(mmc.max as decimal) * 100, 2)
            END
        ) as chapter_progress,
        coalesce(round(mp.page_index / CAST(array_length(mc.pages, 1) as decimal), 2), 0) * 100 as page_progress,
        CAST(coalesce((
            SELECT 1
            FROM manga_favourites mf
            WHERE mf.profile_id = m.profile_id AND mf.manga_id = m.id
        ), 0) AS BIT) as favourite,
        coalesce(mb.pages, '{}') as bookmarks
    FROM touched_manga m
    LEFT JOIN progress mp ON mp.manga_id = m.id
    LEFT JOIN max_chapter_numbers mmc ON mmc.manga_id = m.id
    LEFT JOIN chapter_numbers mc ON
        (mp.id IS NOT NULL AND mc.id = mp.manga_chapter_id) OR
        (mp.id IS NULL AND mmc.first_chapter_id = mc.id)
    LEFT JOIN manga_bookmarks mb ON mb.manga_chapter_id = mc.id
    WHERE
        m.deleted_at IS NULL AND
        mp.deleted_at IS NULL
)
SELECT
    DISTINCT
    r.*,
    CAST((CASE WHEN r.chapter_progress >= 100 THEN 1 ELSE 0 END) AS BIT) as completed
FROM records r;
END
$$;