CREATE OR REPLACE FUNCTION get_manga(platformId text, state int)
    RETURNS TABLE (
        manga_id BIGINT,
        manga_chapter_id BIGINT,
        first_chapter_id BIGINT,
        progress_chapter_id BIGINT,
        progress_id BIGINT,
        max_chapter_num INTEGER,
        chapter_num INTEGER,
        page_count INT,
        chapter_progress NUMERIC,
        page_progress NUMERIC,
        favourite BOOLEAN,
        bookmarks INT[],
        has_bookmarks BOOLEAN,
        profile_id BIGINT,
        latest_chapter TIMESTAMP,
        completed BOOLEAN
    )
LANGUAGE plpgsql
AS $$
BEGIN
RETURN QUERY
    WITH progress AS (
        SELECT e.*
        FROM manga_progress_ext e
        JOIN profiles p ON p.id = e.profile_id
        WHERE p.platform_id = platformId
    )
    SELECT
        m.id as manga_id,
        c.id as manga_chapter_id,
        s.first_chapter_id as first_chapter_id,
        t.progress_chapter_id as progress_chapter_id,
        t.progress_id as progress_id,
        s.max_chapter_row_num as max_chapter_num,
        c.ordinal_index as chapter_num,
        COALESCE(array_length(c.pages, 1), 0) as page_count,
        t.chapter_progress as chapter_progress,
        t.page_progress as page_progress,
        COALESCE(t.favourite, FALSE) as favourite,
        t.bookmarks as bookmarks,
        COALESCE(t.has_bookmarks, FALSE) as has_bookmarks,
        t.profile_id as profile_id,
        s.latest_chapter as latest_chapter,
        COALESCE(t.completed, FALSE) as completed
    FROM manga m
    LEFT JOIN progress t ON t.manga_id = m.id
    LEFT JOIN manga_stats s ON s.manga_id = m.id
    LEFT JOIN manga_chapter c ON
        (t.progress_chapter_id IS NOT NULL AND c.id = t.progress_chapter_id) OR
        (t.progress_chapter_id IS NULL AND c.id = s.first_chapter_id)
    WHERE m.deleted_at IS NULL AND (
        (t.manga_id IS NOT NULL AND t.favourite = true AND state = 1) OR
        (t.manga_id IS NOT NULL AND t.completed = true AND state = 2) OR
        (t.manga_id IS NOT NULL AND t.in_progress = true AND t.completed = false AND t.profile_id IS NOT NULL AND state = 3) OR
        (t.manga_id IS NOT NULL AND t.has_bookmarks = true AND state = 4) OR
        (state = 5 AND t.manga_id IS NULL OR (t.favourite = false AND t.profile_id IS NULL and t.has_bookmarks = false)) OR
        (state = 6 AND t.manga_id IS NOT NULL AND NOT (t.favourite = false AND t.profile_id IS NULL and t.has_bookmarks = false)) OR
        state NOT BETWEEN 1 AND 6
    );
END
$$;