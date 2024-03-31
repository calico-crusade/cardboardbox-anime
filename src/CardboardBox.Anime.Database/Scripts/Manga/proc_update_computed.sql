CREATE OR REPLACE PROCEDURE update_computed()
LANGUAGE SQL
AS $$
    CALL update_manga_stats();
    CALL update_manga_progress_ext();
    REFRESH MATERIALIZED VIEW manga_similar_tags;
$$;