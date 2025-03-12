using System.Collections.Specialized;
using System.Web;

namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public class SolverRequest
{
    public const string CMD_GET = "request.get";
    public const string CMD_POST = "request.post";
    public const string CMD_SESSION_CREATE = "sessions.create";
    public const string CMD_SESSION_DESTROY = "sessions.destroy";
    public const string CMD_SESSION_LIST = "sessions.list";

    [JsonPropertyName("cmd")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; } = null;

    [JsonPropertyName("maxTimeout")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxTimeout { get; set; } = 0;

    [JsonPropertyName("session")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }

    [JsonPropertyName("cookies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SolverCookie[]? Cookies { get; set; } = null;

    [JsonPropertyName("returnOnlyCookies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ReturnOnlyCookies { get; set; } = false;

    [JsonPropertyName("postData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StrPostData { get; set; } = null;

    [JsonPropertyName("proxy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SolverProxy? Proxy { get; set; } = null;

    [JsonIgnore]
    public NameValueCollection PostData
    {
        get
        {
            if (string.IsNullOrEmpty(StrPostData)) return [];
            return HttpUtility.ParseQueryString(StrPostData);
        }
        set
        {
            if (value is null || value.Count == 0)
            {
                StrPostData = null;
                return;
            }

            StrPostData = string.Join("&", value.AllKeys.Select(t => $"{t}={HttpUtility.UrlEncode(value[t])}"));
        }
    }
}
