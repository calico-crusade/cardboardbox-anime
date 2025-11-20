CREATE OR REPLACE PROCEDURE update_chapter_computed()
LANGUAGE SQL
AS $$
    WITH chapter_ordinals_only AS (
        SELECT
            c.ordinal,
            c.manga_id,
            MIN(c.created_at) as created_at
        FROM manga_chapter c
        WHERE c.deleted_at IS NULL
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

    REFRESH MATERIALIZED VIEW manga_chapter_grouped;
$$;