using System.Collections.Specialized;

namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public class SolverSession(
    IFlareSolverApiService _api, 
    string _sessionId) : IAsyncDisposable
{
    public Task<SolverResponse?> Get(string url, SolverCookie[]? cookies = null, SolverProxy? proxy = null)
    {
        return _api.Get(url, cookies: cookies, proxy: proxy, sessionId: _sessionId);
    }

    public Task<SolverResponse?> Post(string url, NameValueCollection data, SolverCookie[]? cookies = null, SolverProxy? proxy = null)
    {
        return _api.Post(url, data, cookies: cookies, proxy: proxy, sessionId: _sessionId);
    }

    public Task<SolverResponse?> Post(string url, Dictionary<string, string> data, SolverCookie[]? cookies = null, SolverProxy? proxy = null)
    {
        var collection = new NameValueCollection();
        foreach (var (key, value) in data)
        {
            collection.Add(key, value);
        }
        return Post(url, collection, cookies: cookies, proxy: proxy);
    }

    public async ValueTask DisposeAsync()
    {
        await _api.SessionDestroy(_sessionId);
        GC.SuppressFinalize(this);
    }
}
