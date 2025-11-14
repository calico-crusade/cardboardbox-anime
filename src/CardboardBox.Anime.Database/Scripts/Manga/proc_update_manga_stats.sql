CREATE OR REPLACE PROCEDURE update_manga_stats()
LANGUAGE SQL
AS $$
   TRUNCATE TABLE manga_stats;

   WITH chapter_ordinals_only AS (
        SELECT
            c.ordinal,
            c.manga_id,
            MIN(c.created_at) as created_at
        FROM manga_chapter c
        GROUP BY c.manga_id, c.ordinal
    ), chapter_numbers AS (
        SELECT
            c.ordinal,
            row_number() over (
                PARTITION BY c.manga_id
                ORDER BY c.ordinal, c.created_at
            ) as row_num,
            c.manga_id
        FROM chapter_ordinals_only c
    )
    UPDATE manga_chapter c
    SET ordinal_index = t.row_num
    FROM chapter_numbers t
    WHERE 
        t.ordinal = c.ordinal AND 
        t.manga_id = c.manga_id;

    WITH manga_grouped AS (
        SELECT
            MAX(ordinal) as last_chapter,
            MIN(ordinal) as first_chapter,
            MAX(ordinal_index) as row_num,
            COUNT(DISTINCT ordinal) as unique_chapter_count,
            COUNT(ordinal) as chapter_count,
            MAX(created_at) as latest_chapter,
            manga_id
        FROM manga_chapter
        WHERE deleted_at IS NULL
        GROUP BY manga_id
        ORDER BY manga_id
    ), temp_latest_grouping AS (
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
        JOIN manga_grouped mg ON mg.manga_id = m.id
        WHERE m.deleted_at IS NULL
    )
    INSERT INTO manga_stats
    SELECT * FROM temp_latest_grouping;
$$;