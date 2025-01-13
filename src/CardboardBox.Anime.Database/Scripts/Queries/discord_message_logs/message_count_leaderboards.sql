SELECT
    '<@' || author_id || '>' as author_id,
    '<t:' || TRUNC(EXTRACT(epoch FROM MAX(created_at))) || ':R>' as most_recent_message,
    '<t:' || TRUNC(EXTRACT(epoch FROM MIN(created_at))) || ':R>' as oldest_message,
    COUNT(*) as message_count
FROM discord_message_logs
WHERE guild_id = '440005411815424000'
GROUP BY author_id
ORDER BY COUNT(*) DESC;