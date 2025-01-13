; WITH word_splits AS (
    SELECT
        message_id,
        author_id,
        channel_id,
        UNNEST(
            regexp_split_to_array(
                LOWER(content),
                '\s+|,|\.'
            )
        ) as word,
        created_at
    FROM discord_message_logs
    WHERE
        guild_id = '440005411815424000' AND
        TRIM(content) <> ''
), word_sanitized AS (
    SELECT
        message_id,
        author_id,
        channel_id,
        created_at,
        TRIM(REGEXP_REPLACE(word, '[^a-z0-9]', '', 'g')) as word
    FROM word_splits
), word_counts AS (
    SELECT
        author_id,
        MAX(created_at) as latest_message_ts,
        MIN(created_at) as oldest_message_ts,
        word,
        COUNT(*) as count
    FROM word_sanitized
    WHERE TRIM(word) <> ''
    GROUP BY author_id, word
), ordered_words AS (
    SELECT
        *,
        ROW_NUMBER() OVER (
            PARTITION BY author_id
            ORDER BY count DESC
        ) as order_number
    FROM word_counts
)
SELECT *
FROM ordered_words
WHERE order_number < 11
ORDER BY author_id, order_number DESC;