using Serilog.Core;
using Serilog.Events;

namespace CardboardBox.Anime.Cli.Interactive;

public class EventSink(IFormatProvider? provider = null) : ILogEventSink
{
    public delegate void LogMessage(MessageData message);

    public static event LogMessage OnLog = (message) => { };

    private readonly IFormatProvider? _provider = provider;

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(_provider);
        OnLog(new(logEvent.Timestamp.DateTime.ToUniversalTime(), message, logEvent.Level.ToString()));
    }
}

public record class MessageData(
    DateTime Timestamp,
    string Message,
    string Level);
