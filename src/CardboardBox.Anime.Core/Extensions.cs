using CardboardBox.Database;
using CardboardBox.Http;
using HtmlAgilityPack;
using System.Linq.Expressions;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CardboardBox.Anime
{
	using Core.Models;

	public static class Extensions
	{
		public static T Bind<T>(this IConfiguration config, string? section = null)
		{
			var i = Activator.CreateInstance<T>();
			var target = string.IsNullOrEmpty(section) ? config : config.GetSection(section);
			target.Bind(i);
			return i;
		}

		public static string MD5Hash(this string data)
		{
			using var md5 = MD5.Create();
			var input = Encoding.UTF8.GetBytes(data);
			var output = md5.ComputeHash(input);
			return Convert.ToHexString(output);
		}

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

		public static async Task<List<T>> ToList<T>(this Task<IAsyncCursor<T>> task)
		{
			return await (await task).ToListAsync();
		}

		public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
		{
			return Task.WhenAll(tasks);
		}

		public static Anime Clean(this Anime anime)
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

		public static IEnumerable<Anime> Clean(this IEnumerable<Anime> anime)
		{
			foreach (var item in anime)
				yield return item.Clean();
		}

		public static async Task<HtmlDocument?> GetHtml(this IHttpBuilder builder)
		{
			using var resp = await builder.Result();
			if (resp == null || !resp.IsSuccessStatusCode) return null;

			var data = await resp.Content.ReadAsStringAsync();
			return data.ParseHtml();
		}

		public static HtmlDocument ParseHtml(this string html)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			return doc;
		}

		public static HtmlNode Copy(this HtmlNode node)
		{
			return node.InnerHtml.ParseHtml().DocumentNode;
		}

		public static string HtmlDecode(this string data)
		{
			return WebUtility.HtmlDecode(data);
		}
	}

	public class PaginatedResult<T>
	{
		[JsonPropertyName("pages")]
		public int Pages { get; set; }

		[JsonPropertyName("count")]
		public int Count { get; set; }

		[JsonPropertyName("results")]
		public T[] Results { get; set; } = Array.Empty<T>();

		public PaginatedResult() { }

		public PaginatedResult(int pages, int count, T[] results)
		{
			Pages = pages;
			Count = count;
			Results = results;
		}

		public void Deconstruct(out int pages, out int count, out T[] results)
		{
			pages = Pages;
			count = Count;
			results = Results;
		}
	}
}