using DiscordClient = Discord.WebSocket.DiscordSocketClient;

namespace CardboardBox.Anime.Bot;

public interface IScriptMethods
{
    void Info(string message, params object[]? args);
    void Warning(string message, params object[]? args);
    void Error(string message, params object[]? args);
    SocketGuild[] Guilds();
}

public class ScriptMethods : IScriptMethods
{
    private readonly DiscordClient _client;
    private readonly ILogger _logger;

    public ScriptMethods(
        DiscordClient client,
        ILogger<ScriptMethods> logger)
    {
        _client = client;
        _logger = logger;
    }

    public void Info(string message, params object[]? args)
    {
        _logger.LogInformation(message, args ?? Array.Empty<object>());
    }

    public void Warning(string message, params object[]? args)
    {
        _logger.LogWarning(message, args ?? Array.Empty<object>());
    }

    public void Error(string message, params object[]? args)
    {
        _logger.LogError(message, args ?? Array.Empty<object>());
    }

    public SocketGuild[] Guilds() => _client.Guilds.ToArray();
}
