CREATE OR REPLACE FUNCTION get_manga_filtered(platformId text, state int, ids bigint[])
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
        favourite BOOLEAN,
        bookmarks INT[],
        has_bookmarks BOOLEAN,
        profile_id BIGINT,
        latest_chapter TIMESTAMP,
        progress_removed BOOLEAN,
        completed BOOLEAN
    )
LANGUAGE plpgsql
AS $$
BEGIN
 RETURN QUERY WITH chapter_numbers AS (
    SELECT
        c.*,
        row_number() over (
            PARTITION BY c.manga_id
            ORDER BY c.ordinal ASC, c.created_at ASC
        ) as row_num
    FROM manga_chapter c
    JOIN manga m ON m.id = c.manga_id
    WHERE m.id = ANY(ids) AND m.deleted_at IS NULL AND c.deleted_at IS NULL
), max_chapter_numbers AS (
    SELECT
        c.manga_id,
        MAX(c.row_num) as max,
        MIN(c.id) as first_chapter_id,
        MAX(created_at) as latest_chapter
    FROM chapter_numbers c
    GROUP BY c.manga_id
), progress AS (
    SELECT
        mp.*
    FROM manga_progress mp
    JOIN profiles p ON p.id = mp.profile_id
    WHERE
        p.platform_id = platformId AND
        mp.manga_id = ANY(ids)
), records AS (
    SELECT DISTINCT
        m.id as manga_id,
        coalesce(mc.id, mmc.first_chapter_id) as manga_chapter_id,
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
        coalesce(round((mp.page_index + 1) / CAST(array_length(mc.pages, 1) as decimal), 2), 0) * 100 as page_progress,
        coalesce((
            SELECT true
            FROM manga_favourites mf
            JOIN profiles p ON mf.profile_id = p.id
            WHERE p.platform_id = platformId AND mf.manga_id = m.id
        ), false) as favourite,
        coalesce(mb.pages, '{}') as bookmarks,
        coalesce((
            SELECT true
            FROM manga_bookmarks mbc
            JOIN profiles p ON mbc.profile_id = p.id
            WHERE mbc.manga_id = m.id AND p.platform_id = platformId
            LIMIT 1
        ), false) as has_bookmarks,
        mp.profile_id as profile_id,
        mmc.latest_chapter,
        mp.page_index IS NULL as progress_removed
    FROM manga m
    LEFT JOIN progress mp ON mp.manga_id = m.id
    LEFT JOIN max_chapter_numbers mmc ON mmc.manga_id = m.id
    LEFT JOIN chapter_numbers mc ON
        (mp.id IS NOT NULL AND mc.id = mp.manga_chapter_id) OR
        (mp.id IS NULL AND mmc.first_chapter_id = mc.id)
    LEFT JOIN manga_bookmarks mb ON mb.manga_chapter_id = mc.id
    WHERE
        m.deleted_at IS NULL AND
        m.id = ANY(ids)
), all_records AS (
    SELECT
        DISTINCT
        r.*,
        r.chapter_progress >= 100 as completed
    FROM records r
)
SELECT * FROM all_records t
WHERE
    (t.favourite = true AND (state = 1 OR state = 6)) OR
    (t.completed = true AND (state = 2 OR state = 6)) OR
    (t.completed = false AND t.profile_id IS NOT NULL AND t.progress_removed = false AND (state = 3 OR state = 6)) OR
    (t.has_bookmarks = true AND (state = 4 OR state = 6)) OR
    (state = 5 AND t.favourite = false AND t.profile_id IS NULL and t.has_bookmarks = false) OR
    state NOT IN (1, 2, 3, 4, 5, 6);
END
$$;