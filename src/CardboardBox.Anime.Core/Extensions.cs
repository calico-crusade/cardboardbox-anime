using System.Linq.Expressions;
using System.Web;

namespace CardboardBox;

using AnimeM = Anime.Core.Models.Anime;

public static class Extensions
{
	public const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36";

	public static AnimeM Clean(this AnimeM anime)
	{
		var replaces = new Dictionary<string[], string[]>
		{
			[new[] { "sci-fi", "fantasy" }] = new[] { "sci fi and fantasy" },
			[new[] { "sci-fi", "action" }] = new[] { "science fiction - action" },
			[new[] { "sci-fi" }] = new[] { "sci fi", "animation - science fiction", "science fiction", "science fiction - comic" },
			[new[] { "action", "adventure" }] = new[] { "action and adventure", "action/adventure" },
			[new[] { "live-action" }] = new[] { "live action" },
			[new[] { "comedy" }] = new[] { "comedy – animation" },
			[new[] { "romance", "comedy" }] = new[] { "comedy – romance" },
			[new[] { "anime" }] = new[] { "animation - anime" },
			[new[] { "mystery", "thriller" }] = new[] { "mystery and thriller" },
			[new[] { "adventure" }] = new[] { "adventure - comic" },
			[new[] { "action" }] = new[] { "action - sword and sandal", "action - comic" }
		};

		var delParan = (string item) =>
		{
			if (!item.Contains('(')) return item.ToLower().Trim();
			return item.Split('(').First().Trim().ToLower();
		};

		var tagFix = (string item) =>
		{
			var value = item.ToLower().Trim();
			foreach (var (key, vals) in replaces)
				if (vals.Contains(value)) return key;
			return new[] { value };
		};

		anime.Metadata.Languages = anime.Metadata.Languages.Select(delParan).Distinct().ToList();
		anime.Metadata.Ratings = anime.Metadata.Ratings.Select(t => t.ToLower().Trim().Split('|')).SelectMany(t => t).Distinct().ToList();
		anime.Metadata.Tags = anime.Metadata.Tags.Select(tagFix).SelectMany(t => t).Distinct().ToList();

		anime.Images = anime.Images.Select(t =>
		{
			if (t.Source.Contains("https:")) return t;
			t.Source = t.Source.Replace("https", "https:");
			return t;
		}).ToList();

		return anime;
	}

	public static IEnumerable<AnimeM> Clean(this IEnumerable<AnimeM> anime)
	{
		foreach (var item in anime)
			yield return item.Clean();
	}

	public static T Bind<T>(this IConfiguration config, string? section = null)
	{
		var i = Activator.CreateInstance<T>();
		var target = string.IsNullOrEmpty(section) ? config : config.GetSection(section);
		target.Bind(i);
		return i;
	}
			//
	public static async Task<PaginatedResult<T>> Paginate<T>(
		this IMongoService<T> mongo,
		int page, int size,
		Expression<Func<T, object>> sort,
		bool ascending = true,
		FilterDefinition<T>? filter = null)
	{
		var countFacet = AggregateFacet.Create("count",
			PipelineDefinition<T, AggregateCountResult>.Create(new[]
			{
				PipelineStageDefinitionBuilder.Count<T>()
			}));

		var dataFacet = AggregateFacet.Create("data",
			PipelineDefinition<T, T>.Create(new[]
			{
				PipelineStageDefinitionBuilder.Sort(ascending ? Builders<T>.Sort.Ascending(sort) : Builders<T>.Sort.Descending(sort)),
				PipelineStageDefinitionBuilder.Skip<T>((page - 1) * size),
				PipelineStageDefinitionBuilder.Limit<T>(size)
			}));

		filter ??= mongo.Filter.Empty;
		var ag = (await mongo.Collection.Aggregate()
			.Match(filter)
			.Facet(countFacet, dataFacet)
			.ToListAsync()).First();

		var count = ag.Facets.First(t => t.Name == "count")
			.Output<AggregateCountResult>()?
			.FirstOrDefault()?
			.Count ?? 0;

		var total = (int)count / size;
		var data = ag.Facets.First(t => t.Name == "data").Output<T>();

		return new (total, (int)count, data.ToArray());
	}

	//
	public static async Task<List<T>> ToList<T>(this Task<IAsyncCursor<T>> task)
	{
		return await (await task).ToListAsync();
	}

	//
	public static async Task<HtmlDocument?> GetHtml(this IHttpBuilder builder)
	{
		using var resp = await builder.Result();
		if (resp == null || !resp.IsSuccessStatusCode) return null;

		var data = await resp.Content.ReadAsStringAsync();
		return data.ParseHtml();
	}
	//
	public static HtmlDocument ParseHtml(this string html)
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(html);
		return doc;
	}
	//
	public static HtmlNode Copy(this HtmlNode node)
	{
		return node.InnerHtml.ParseHtml().DocumentNode;
	}
	//
	public static string HTMLDecode(this string text)
	{
		return HttpUtility.HtmlDecode(text).Trim('\n');
	}

	public static string SafeSubString(this string text, int length, int start = 0)
	{
		if (start + length > text.Length)
			return text[start..];

		return text.Substring(start, length);
	}
	//
	public static string GetRootUrl(this string url)
	{
		if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
			throw new UriFormatException(url);

		return uri.GetRootUrl();
	}
	//
	public static string GetRootUrl(this Uri uri)
	{
		var port = uri.IsDefaultPort ? "" : ":" + uri.Port;
		return $"{uri.Scheme}://{uri.Host}{port}";
	}
	//
	public static string? InnerText(this HtmlDocument doc, string xpath)
	{
		return doc.DocumentNode.InnerText(xpath);
	}
	//
	public static string? InnerHtml(this HtmlDocument doc, string xpath)
	{
		return doc.DocumentNode.InnerHtml(xpath);
	}
	//
	public static string? Attribute(this HtmlDocument doc, string xpath, string attr)
	{
		return doc.DocumentNode.Attribute(xpath, attr);
	}
	//
	public static string? InnerText(this HtmlNode doc, string xpath)
	{
		
		return doc.SelectSingleNode(xpath)?.InnerText?.HTMLDecode();
	}
	//
	public static string? InnerHtml(this HtmlNode doc, string xpath)
	{
		return doc.SelectSingleNode(xpath)?.InnerHtml?.HTMLDecode();
	}
	//
	public static string? Attribute(this HtmlNode doc, string xpath, string attr)
	{
		return doc.SelectSingleNode(xpath)?.GetAttributeValue(attr, "")?.HTMLDecode();
	}

	//
	public static async Task<HtmlDocument> GetHtml(this IApiService api, string url, Action<HttpRequestMessage>? config = null)
	{
		var req = await api.Create(url)
			.Accept("text/html")
			.With(c =>
			{
				c.Headers.Add("user-agent", USER_AGENT);
				config?.Invoke(c);
			})
			.Result() ?? throw new NullReferenceException($"Request returned null for: {url}");
		req.EnsureSuccessStatusCode();

		using var io = await req.Content.ReadAsStreamAsync();
		var doc = new HtmlDocument();
		doc.Load(io);

		return doc;
	}
	//
	public static async Task<(Stream data, long length, string filename, string type)> GetData(this IApiService api, string url, Action<HttpRequestMessage>? config = null)
	{
		var req = await api.Create(url)
			.Accept("*/*")
			.With(c =>
			{
				c.Headers.Add("user-agent", USER_AGENT);
				config?.Invoke(c);
			})
			.Result();
		if (req == null)
			throw new NullReferenceException($"Request returned null for: {url}");

		req.EnsureSuccessStatusCode();

		var headers = req.Content.Headers;
		var path = headers?.ContentDisposition?.FileName ?? headers?.ContentDisposition?.Parameters?.FirstOrDefault()?.Value ?? "";
		var type = headers?.ContentType?.ToString() ?? "";
		var length = headers?.ContentLength ?? 0;

		return (await req.Content.ReadAsStreamAsync(), length, path, type);
	}
			
	//
	public static async Task<(T1 item1, T2 item2, T3 item3, T4 item4)[]> QueryAsync<T1, T2, T3, T4>(this ISqlService sql, string query, object? parameters = null, string splitOn = "split")
	{
		using var con = sql.CreateConnection();
		return (await con.QueryAsync<T1, T2, T3, T4, (T1, T2, T3, T4)>(query,
			(a, b, c, d) => (a, b, c, d),
			parameters,
			splitOn: splitOn)).ToArray();
	}
	//
	public static async Task<(T1 item1, T2 item2, T3 item3)[]> QueryAsync<T1, T2, T3>(this ISqlService sql, string query, object? parameters = null, string splitOn = "split")
	{
		using var con = sql.CreateConnection();
		return (await con.QueryAsync<T1, T2, T3, (T1, T2, T3)>(query,
			(a, b, c) => (a, b, c),
			parameters,
			splitOn: splitOn)).ToArray();
	}
	//
	public static async Task<(T1 item1, T2 item2)[]> QueryAsync<T1, T2>(this ISqlService sql, string query, object? parameters = null, string splitOn = "split")
	{
		using var con = sql.CreateConnection();
		return (await con.QueryAsync<T1, T2, (T1, T2)>(query,
			(a, b) => (a, b),
			parameters,
			splitOn: splitOn)).ToArray();
	}
}

//
public class PaginatedResult<T>
{
	[JsonPropertyName("pages")]
	public long Pages { get; set; }

	[JsonPropertyName("count")]
	public long Count { get; set; }

	[JsonPropertyName("results")]
	public T[] Results { get; set; } = Array.Empty<T>();

	public PaginatedResult() { }

	public PaginatedResult(int pages, int count, T[] results)
	{
		Pages = pages;
		Count = count;
		Results = results;
	}

	public PaginatedResult(long pages, long count, T[] results)
	{
		Pages = pages;
		Count = count;
		Results = results;
	}

	public void Deconstruct(out long pages, out long count, out T[] results)
	{
		pages = Pages;
		count = Count;
		results = Results;
	}
}