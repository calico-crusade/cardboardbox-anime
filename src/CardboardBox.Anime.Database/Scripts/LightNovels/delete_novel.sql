DELETE FROM ln_chapter_pages WHERE page_id IN (SELECT id FROM ln_pages WHERE series_id = :seriesId);
DELETE FROM ln_chapters WHERE book_id IN (SELECT id FROM ln_books WHERE series_id = :seriesId);
DELETE FROM ln_books WHERE series_id = :seriesId;
DELETE FROM ln_pages WHERE series_id = :seriesId;
DELETE FROM ln_series WHERE id = :seriesId;