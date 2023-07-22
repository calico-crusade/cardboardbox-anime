CREATE OR REPLACE PROCEDURE update_manga_progress_ext()
LANGUAGE SQL
AS $$

    TRUNCATE TABLE manga_progress_ext;

    WITH touched AS (
        SELECT
            DISTINCT *
        FROM (
            SELECT manga_id, profile_id FROM manga_favourites
            UNION
            SELECT manga_id, profile_id FROM manga_bookmarks
            UNION
            SELECT manga_id, profile_id FROM manga_progress
        ) x
    ), chapter_numbers AS (
        SELECT
            c.*,
            row_number() OVER (
                PARTITION BY c.manga_id
                ORDER BY c.ordinal ASC
            ) AS row_num
        FROM manga_chapter c
        JOIN touched m ON m.manga_id = c.manga_id
    ), max_chapter_numbers AS (
        SELECT
            c.manga_id,
            MAX(c.row_num) AS max,
            MIN(c.id) AS first_chapter_id
        FROM chapter_numbers c
        GROUP BY c.manga_id
    ), progress AS (
        SELECT
            mp.*,
            mmc.max AS max_chapter_num,
            mc.row_num AS chapter_num,
            COALESCE(ARRAY_LENGTH(mc.pages, 1), 0) AS page_count,
            (
                CASE
                    WHEN mmc.first_chapter_id = mc.id AND mp.page_index IS NULL THEN 0
                    WHEN mc.id = s.last_chapter_id THEN 100
                    ELSE LEAST(ROUND(mc.row_num / CAST(mmc.max AS DECIMAL) * 100, 2), 100)
                END
            ) AS chapter_progress,
            LEAST((COALESCE(ROUND((mp.page_index + 1) / CAST(ARRAY_LENGTH(mc.pages, 1) AS DECIMAL), 2), 0) * 100), 100) AS page_progress,
            row_number() OVER (
                PARTITION BY mp.id
                ORDER BY mp.id ASC
            ) AS row_number
        FROM manga_progress mp
        JOIN manga_stats s ON s.manga_id = mp.manga_id
        JOIN max_chapter_numbers mmc ON mmc.manga_id = mp.manga_id
        JOIN chapter_numbers mc ON mc.id = mp.manga_chapter_id
    ), bookmark_count AS (
        SELECT
            manga_id,
            profile_id,
            SUM(ARRAY_LENGTH(pages, 1)) AS count
        FROM manga_bookmarks
        GROUP BY manga_id, profile_id
    ), temp_manga_progress_ext AS (
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
        ORDER BY t.profile_id ASC, t.manga_id DESC
    )
    INSERT INTO manga_progress_ext
    SELECT * FROM temp_manga_progress_ext;
$$;

