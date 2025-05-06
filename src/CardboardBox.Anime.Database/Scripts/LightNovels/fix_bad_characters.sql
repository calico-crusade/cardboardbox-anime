; WITH replacements AS (
    SELECT * FROM (
        VALUES
            ('“', '"', 1),
            ('”', '"', 2),
            ('…', '...', 3),
            ('’', '''', 4),
            ('-', '-', 5)
    ) as t (target, value, ordinal)
), full_replacers AS (
    SELECT
        id,
        full_replace
        (
            content,
            ARRAY(SELECT target FROM replacements ORDER BY ordinal DESC)::text[],
            ARRAY(SELECT value FROM replacements ORDER BY ordinal DESC)::text[]
        )
        as fixed_content,
        content
    FROM ln_pages
    WHERE series_id = 121 --Put the series ID here
)
UPDATE ln_pages p
SET
    content = a.fixed_content
FROM full_replacers a
WHERE a.id = p.id;