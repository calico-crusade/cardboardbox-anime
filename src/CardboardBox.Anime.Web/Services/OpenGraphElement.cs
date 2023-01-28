using System.Net;
using System.Text;

namespace CardboardBox.Anime.Web.Services;

public class OpenGraphElement
{
	public string Tag { get; set; } = "meta";

	public List<(string Key, string Value)> Properties { get; set; } = new();

	public OpenGraphElement Add(string key, string value)
	{
		Properties.Add((key, value));
		return this;
	}

	public OpenGraphElement Add(params (string Key, string Value)[] properties)
	{
		Properties.AddRange(properties);
		return this;
	}

	public string Render()
	{
		var replacers = (string text) => text.Replace("\"", "").Replace("\r\n", "").Replace("\n", "");
		var attrs = Properties.Select(t => $"{t.Key}=\"{replacers(t.Value)}\"");
		return $"<{Tag} {string.Join(" ", attrs)}>";
	}
}

public class OpenGraphBuilder
{
	public List<OpenGraphElement> Elements { get; set; } = new();

	public OpenGraphBuilder Add(Action<OpenGraphElement> builder, string? tag = null)
	{
		var el = new OpenGraphElement();
		if (!string.IsNullOrEmpty(tag))
			el.Tag = tag;

		builder(el);
		Elements.Add(el);
		return this;
	}

	public OpenGraphBuilder AddPropertyContent(string property, string content)
	{
		return Add((e) => e.Add("property", property).Add("content", content));
	}

	public OpenGraphBuilder Description(string content) => AddPropertyContent("description", content).AddPropertyContent("og:description", content);
	public OpenGraphBuilder Title(string title) => AddPropertyContent("og:title", title);
	public OpenGraphBuilder Locale(string locale) => AddPropertyContent("og:locale", locale);
	public OpenGraphBuilder Type(string type) => AddPropertyContent("og:type", type);
	public OpenGraphBuilder Url(string url) => AddPropertyContent("og:url", url);
	public OpenGraphBuilder SiteName(string name) => AddPropertyContent("og:site_name", name);
	public OpenGraphBuilder Image(string url) => AddPropertyContent("og:image", url);
	public OpenGraphBuilder PublishTime(DateTime dt) => AddPropertyContent("article:published_time", dt.ToString("yyyy-MM-ddTHH:mm:ssK"));
	public OpenGraphBuilder ModifiedTime(DateTime dt) => AddPropertyContent("article:modified_time", dt.ToString("yyyy-MM-ddTHH:mm:ssK"));
	public OpenGraphBuilder ImageWidth(int width) => AddPropertyContent("og:image:width", width.ToString());
	public OpenGraphBuilder ImageHeight(int height) => AddPropertyContent("og:image:height", height.ToString());
	public OpenGraphBuilder ImageMimeType(string type) => AddPropertyContent("og:image:type", type);
	public OpenGraphBuilder Author(string author) => AddPropertyContent("author", author);

	public OpenGraphBuilder ProxiedImage(string url)
	{
		var encoded = WebUtility.UrlEncode(url);
		var uri = $"https://cba-proxy.index-0.com/proxy?path={encoded}&group=ogp";
		return Image(uri);
	}

	public string Render()
	{
		return string.Join("\r\n\t", Elements.Select(t => t.Render()));
	}
}

public interface IOpenGraphService
{
	Task<byte[]> Default();
	Task<byte[]> Replace(string target);
	Task<byte[]> Replace(OpenGraphBuilder ogbob);
	Task<byte[]> Replace(Action<OpenGraphBuilder> bob);
}

public class OpenGraphService : IOpenGraphService
{
	private static string? _indexContent;
	private const string REPLACER = "<!--SEO OUTLET-->";

	public Task<byte[]> Default() => Replace("");
	public async Task<byte[]> Replace(string target)
	{
		_indexContent ??= await File.ReadAllTextAsync("wwwroot/index.html");
		var data = _indexContent.Replace(REPLACER, target);
		return Encoding.UTF8.GetBytes(data);
	}

	public Task<byte[]> Replace(OpenGraphBuilder ogbob)
	{
		var res = ogbob.Render();
		return Replace(res);
	}

	public Task<byte[]> Replace(Action<OpenGraphBuilder> bob)
	{
		var ogb = new OpenGraphBuilder();
		bob(ogb);
		return Replace(ogb);
	}
}
