namespace CardboardBox.Manga;

using Match;
using Providers;
using System.Collections.Generic;

public static class Extensions
{
	public static IServiceCollection AddManga(this IServiceCollection services)
	{
		return services
			.AddTransient<IMangaService, MangaService>()

			.AddTransient<IMangakakalotTvSource, MangakakalotTvSource>()
			.AddTransient<IMangakakalotComSource, MangakakalotComSource>()
			.AddTransient<IMangakakalotComAltSource, MangakakalotComAltSource>()
			.AddTransient<IMangaDexSource, MangaDexSource>()
			.AddTransient<IMangaClashSource, MangaClashSource>()
			.AddTransient<INhentaiSource, NhentaiSource>()
			.AddTransient<IBattwoSource, BattwoSource>()
			.AddTransient<IMangaKatanaSource, MangaKatanaSource>()

			.AddTransient<IMangaEpubService, MangaEpubService>()
			.AddTransient<IMangaImageService, MangaImageService>()

			.AddTransient<IMatchApiService, MatchApiService>()
			.AddTransient<ISauceNaoApiService, SauceNaoApiService>()
			.AddTransient<IMangaMatchService, MangaMatchService>()
			.AddTransient<INsfwApiService, NsfwApiService>()

			.AddTransient<IGoogleVisionService, GoogleVisionService>()
			.AddTransient<IMangaSearchService, MangaSearchService>()

			.AddMangadex();
	}

	public static T? PreferedOrFirst<T>(this IEnumerable<T> data, Func<T, bool> prefered)
	{
		foreach(var item in data)
		{
			if (prefered(item)) return item;
		}

		return data.FirstOrDefault();
	}

	public static IEnumerable<T[]> Split<T>(this IEnumerable<T> data, int count)
	{
		var total = (int)Math.Ceiling((decimal)data.Count() / count);
		var current = new List<T>();

		foreach(var item in data)
		{
			current.Add(item);

			if (current.Count == total)
			{
				yield return current.ToArray();
				current.Clear();
			}
		}

		if (current.Count > 0) yield return current.ToArray();
	}

	/// <summary>
	/// Moves the given iterator until if finds a selector that doesn't match
	/// </summary>
	/// <typeparam name="T">The type of data to process</typeparam>
	/// <param name="data">The iterator to process</param>
	/// <param name="previous">The last item for via any previous MoveUntil reference</param>
	/// <param name="selectors">The different properties to check against</param>
	/// <returns>All of the items in the current grouping</returns>
	public static Grouping<T> MoveUntil<T>(this IEnumerator<T> data, T? previous, params Func<T, object?>[] selectors)
	{
		var items = new List<T>();

		//Add the previous item to the collection of items
		if (previous != null) items.Add(previous);
		
		//Keep moving through the iterator until EoC
		while(data.MoveNext())
		{
			//Get the current item
			var current = data.Current;
			//Get the last item
			var last = items.LastOrDefault();

			//No last item? Add current and skip to next item
			if (last == null)
			{
				items.Add(current);
				continue;
			}

			//Iterate through selectors until one matches
			for (var i = 0; i < selectors.Length; i++)
			{
				//Get the keys to check
				var selector = selectors[i];
				var fir = selector(last);
				var cur = selector(current);

				//Check if the keys are the same
				var isSame = (fir == null && cur == null) ||
					(fir != null && fir.Equals(cur));

				//They are the same, move to next selector
				if (isSame) continue;

				//Break out of the check, returning the grouped items and the last item checked
				return new(items.ToArray(), current, i);
			}

			//All selectors are the same, add item to the collection
			items.Add(current);
		}

		//Reached EoC, return items, no last, and no selector index
		return new(items.ToArray(), default, -1);
	}

	/// <summary>
	/// Fetch an index via a predicate
	/// </summary>
	/// <typeparam name="T">The type of data</typeparam>
	/// <param name="data">The data to process</param>
	/// <param name="predicate">The predicate used to find the index</param>
	/// <returns>The index or -1</returns>
	public static int IndexOf<T>(this IEnumerable<T> data, Func<T, bool> predicate)
	{
		int index = 0;
		foreach(var item in data)
		{
			if (predicate(item))
				return index;

			index++;
		}

		return -1;
	}

	/// <summary>
	/// Fetch an index via a predicate (or null if not found)
	/// </summary>
	/// <typeparam name="T">The type of data</typeparam>
	/// <param name="data">The data to process</param>
	/// <param name="predicate">The predicate used to find the index</param>
	/// <returns>The index or null</returns>
	public static int? IndexOfNull<T>(this IEnumerable<T> data, Func<T, bool> predicate)
	{
		var idx = data.IndexOf(predicate);
		return idx == -1 ? null : idx;
	}

	public static TOut? Clone<TIn, TOut>(this TIn data) where TOut: TIn
	{
		var ser = JsonSerializer.Serialize(data);
		return JsonSerializer.Deserialize<TOut>(ser);
	}
}

public record class Grouping<T>(T[] Items, T? Last, int Index);
