using System.Collections.Specialized;

namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public interface IFlareSolverApiService
{
    Task<SolverResponse?> Get(string url,
        string? sessionId = null,
        SolverCookie[]? cookies = null,
        SolverProxy? proxy = null,
        bool returnOnlyCookies = false,
        int? maxTimeout = null);

    Task<SolverResponse?> Post(string url,
        NameValueCollection parameters,
        string? sessionId = null,
        SolverCookie[]? cookies = null,
        SolverProxy? proxy = null,
        bool returnOnlyCookies = false,
        int? maxTimeout = null);

    Task<SolverSessionList?> SessionList();

    Task<SolverSessionCreate?> SessionCreate(string? sessionId = null, SolverProxy? proxy = null);

    Task<SolverSessionDestroy?> SessionDestroy(string sessionId);
}

internal class FlareSolverApiService(
    IApiService _api,
    IConfiguration _config) : IFlareSolverApiService
{
    private string? _serverUrl;
    private const int DEFAULT_TIMEOUT = 60_000;

    public string SolverUrl => _config["FlareSolver:Url"]
        ?? throw new InvalidOperationException("FlareSolver:Url is not set in the configuration.");

    public string Version => _config["FlareSolver:Version"] ?? "v1";

    public string ServerUrl => _serverUrl ??= $"{SolverUrl?.TrimEnd('/')}/{Version.Trim('/')}";

    public Task<SolverResponse?> Get(string url,
        string? sessionId = null,
        SolverCookie[]? cookies = null,
        SolverProxy? proxy = null,
        bool returnOnlyCookies = false,
        int? maxTimeout = null)
    {
        var request = new SolverRequest
        {
            Command = SolverRequest.CMD_GET,
            Url = url,
            SessionId = sessionId,
            Cookies = cookies,
            Proxy = proxy,
            MaxTimeout = maxTimeout ?? DEFAULT_TIMEOUT,
            ReturnOnlyCookies = returnOnlyCookies
        };
        return _api.Post<SolverResponse, SolverRequest>(ServerUrl, request);
    }

    public Task<SolverResponse?> Post(string url, 
        NameValueCollection parameters, 
        string? sessionId = null, 
        SolverCookie[]? cookies = null, 
        SolverProxy? proxy = null, 
        bool returnOnlyCookies = false, 
        int? maxTimeout = null)
    {
        var request = new SolverRequest
        {
            Command = SolverRequest.CMD_POST,
            PostData = parameters,
            Url = url,
            SessionId = sessionId,
            Cookies = cookies,
            Proxy = proxy,
            MaxTimeout = maxTimeout ?? DEFAULT_TIMEOUT,
            ReturnOnlyCookies = returnOnlyCookies
        };
        return _api.Post<SolverResponse, SolverRequest>(ServerUrl, request);
    }

    public Task<SolverSessionCreate?> SessionCreate(string? sessionId = null, SolverProxy? proxy = null)
    {
        var request = new SolverRequest
        {
            Command = SolverRequest.CMD_SESSION_CREATE,
            SessionId = sessionId,
            Proxy = proxy
        };
        return _api.Post<SolverSessionCreate, SolverRequest>(ServerUrl, request);
    }

    public Task<SolverSessionDestroy?> SessionDestroy(string sessionId)
    {
        var request = new SolverRequest
        {
            Command = SolverRequest.CMD_SESSION_DESTROY,
            SessionId = sessionId,
        };
        return _api.Post<SolverSessionDestroy, SolverRequest>(ServerUrl, request);
    }

    public Task<SolverSessionList?> SessionList()
    {
        var request = new SolverRequest
        {
            Command = SolverRequest.CMD_SESSION_LIST
        };
        return _api.Post<SolverSessionList, SolverRequest>(ServerUrl, request);
    }
}
