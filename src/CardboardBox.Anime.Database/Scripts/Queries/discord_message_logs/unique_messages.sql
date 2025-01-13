; WITH unique_messages AS (
    SELECT
        *,
        row_number() over (
            PARTITION BY message_id
            ORDER BY created_at DESC
        ) as unique_message_number
    FROM discord_message_logs
    WHERE channel_id = '440005411815424002'
), message_numbers AS (
    SELECT
        *,
        row_number() over (
            ORDER BY created_at DESC
        ) as message_number
    FROM unique_messages
    WHERE unique_message_number = 1
)
SELECT *
FROM message_numbers
WHERE message_number = 1993;