CREATE OR REPLACE PROCEDURE update_manga_stats()
LANGUAGE SQL
AS $$
   TRUNCATE TABLE manga_stats;

    WITH manga_grouped AS (
        SELECT
            MAX(ordinal) as last_chapter,
            MIN(ordinal) as first_chapter,
            COUNT(DISTINCT ordinal) as unique_chapter_count,
            COUNT(ordinal) as chapter_count,
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
                    c.ordinal = mg.last_chapter
                ORDER BY c.created_at DESC
                LIMIT 1
            ) as last_chapter_id,
            mg.last_chapter as last_chapter_ordinal,
            (
                SELECT c.id
                FROM manga_chapter c
                WHERE
                    c.manga_id = m.id AND
                    c.ordinal = mg.first_chapter
                ORDER BY c.created_at ASC
                LIMIT 1
            ) as first_chapter_id,
            mg.first_chapter as first_chapter_ordinal,
            mg.chapter_count as chapter_count,
            mg.unique_chapter_count as unique_chapter_count
        FROM manga m
        JOIN manga_grouped mg ON mg.manga_id = m.id
    )
    INSERT INTO manga_stats
    SELECT * FROM temp_latest_grouping; 
$$;