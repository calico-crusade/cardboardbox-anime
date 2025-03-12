; WITH correct_dates AS (
    SELECT
        DISTINCT
        cur.id as chapter_id,
        cur.source_id as chapter_source_id,
        cac.created_at,
        cac.updated_at
    FROM manga_chapter cur
    JOIN manga_chapter_cache cac ON cac.source_id = cur.source_id
)
UPDATE manga_chapter c
SET
    created_at = n.created_at,
    updated_at = n.updated_at
FROM correct_dates n
WHERE c.id = n.chapter_id;