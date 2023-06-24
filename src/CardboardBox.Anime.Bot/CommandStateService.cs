namespace CardboardBox.Anime.Bot;

public interface ICommandStateService
{
    void Set<T>(ulong? messageId, T data);
    void Set<T>(IMessage? msg, T data);
    T? Get<T>(ulong? messageId);
    T? Get<T>(IMessage? msg);
    void Remove(ulong? messageId);
    void Remove(IMessage? msg);

    Task<(bool, T?)> ValidateState<T>(ComponentHandler handler);
}

public class CommandStateService : ICommandStateService
{
    private readonly Dictionary<ulong, object?> _state = new();

    public void Set<T>(ulong? messageId, T data)
    {
        if (messageId == null) return;

        if (_state.ContainsKey(messageId.Value))
            _state[messageId.Value] = data;
        else
            _state.Add(messageId.Value, data);
    }

    public void Set<T>(IMessage? msg, T data) => Set(msg?.Id, data);

    public T? Get<T>(ulong? messageId)
    {
        if (messageId == null) return default;

        if (!_state.ContainsKey(messageId.Value)) return default;

        var state = _state[messageId.Value];
        if (state == null) return default;

        return (T)state;
    }

    public T? Get<T>(IMessage? msg) => Get<T>(msg?.Id);

    public void Remove(ulong? messageId)
    {
        if (messageId == null) return;

        if (_state.ContainsKey(messageId.Value))
            _state.Remove(messageId.Value);
    }

    public void Remove(IMessage? msg) => Remove(msg?.Id);

    public async Task<(bool, T?)> ValidateState<T>(ComponentHandler handler)
    {
        try
        {
            var state = Get<T>(handler.Message ?? throw new ArgumentNullException(nameof(handler)));
            if (state == null)
            {
                await handler.RemoveComponents(t =>
                {
                    t.Content = "An error occurred, unfortunately, I cannot continue with this request :/";
                });
                return (false, default);
            }

            return (true, state);
        }
        catch
        {
            return (false, default);
        }
    }
}
