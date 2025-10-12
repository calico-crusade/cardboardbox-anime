DELETE FROM ln_chapter_pages WHERE chapter_id IN (
    SELECT
        c.id
    FROM ln_chapters c
    JOIN ln_books b ON c.book_id = b.id
    WHERE b.series_id = :series_id
);
DELETE FROM ln_chapters WHERE book_id IN (
    SELECT id
    FROM ln_books
    WHERE series_id = :series_id
);
DELETE FROM ln_books WHERE series_id = :series_id;
DELETE FROM ln_pages WHERE series_id = :series_id;
DELETE FROM ln_series WHERE id = :series_id;