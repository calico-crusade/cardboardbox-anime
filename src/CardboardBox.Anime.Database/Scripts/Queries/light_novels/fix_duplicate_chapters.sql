; WITH duplicate_chapters AS (
    SELECT
        DISTINCT
        fp.page_id,
        CASE WHEN fp.chapter_id < sp.chapter_id THEN fp.chapter_id
             ELSE sp.chapter_id
        END as chapter_id,
        CASE WHEN fp.chapter_id > sp.chapter_id THEN fp.chapter_id
             ELSE sp.chapter_id
        END as duplicate_chapter_id
    FROM ln_chapter_pages fp
    JOIN ln_chapter_pages sp ON
        fp.page_id = sp.page_id AND
        fp.id <> sp.id
)
DELETE FROM ln_chapter_pages
WHERE chapter_id IN (
    SELECT duplicate_chapter_id
    FROM duplicate_chapters
);

DELETE FROM ln_chapters
WHERE id NOT IN (
    SELECT chapter_id 
    FROM ln_chapter_pages
);