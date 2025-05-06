SELECT
    title,
    url,
    regexp_matches(content, '<img(.*?)/>', 'g') as matches
FROM ln_pages
WHERE
    series_id IN (:seriesId) AND content like '%<img%';