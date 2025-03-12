using System.Collections.Specialized;

namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public interface IFlareSolver
{
    Task<SolverSession> CreateSession(SolverProxy? proxy = null);

    Task<SolverResponse?> Get(string url, SolverCookie[]? cookies = null, SolverProxy? proxy = null, int? timeout = null);

    Task<SolverResponse?> Post(string url, NameValueCollection data, SolverCookie[]? cookies = null, SolverProxy? proxy = null, int? timeout = null);

    Task<SolverResponse?> Post(string url, Dictionary<string, string> data, SolverCookie[]? cookies = null, SolverProxy? proxy = null, int? timeout = null);

    Task ClearSessions();
}

internal class FlareSolverApi(
    IFlareSolverApiService _api) : IFlareSolver
{
    public async Task ClearSessions()
    {
        var sessions = await _api.SessionList();
        if (sessions is null || sessions.SessionIds is null || sessions.SessionIds.Length == 0)
            return;

        foreach (var sessionId in sessions.SessionIds)
            await _api.SessionDestroy(sessionId);
    }

    public async Task<SolverSession> CreateSession(SolverProxy? proxy = null)
    {
        var session = await _api.SessionCreate(proxy: proxy)
            ?? throw new Exception("Failed to create session");

        return new SolverSession(_api, session.SessionId);
    }

    public Task<SolverResponse?> Get(string url, SolverCookie[]? cookies = null, SolverProxy? proxy = null, int? timeout = null)
    {
        return _api.Get(url, cookies: cookies, proxy: proxy, maxTimeout: timeout);
    }

    public Task<SolverResponse?> Post(string url, NameValueCollection data, SolverCookie[]? cookies = null, SolverProxy? proxy = null, int? timeout = null)
    {
        return _api.Post(url, data, cookies: cookies, proxy: proxy, maxTimeout: timeout);
    }

    public Task<SolverResponse?> Post(string url, Dictionary<string, string> data, SolverCookie[]? cookies = null, SolverProxy? proxy = null, int? timeout = null)
    {
        var collection = new NameValueCollection();
        foreach (var (key, value) in data)
        {
            collection.Add(key, value);
        }
        return Post(url, collection, cookies: cookies, proxy: proxy, timeout: timeout);
    }
}
