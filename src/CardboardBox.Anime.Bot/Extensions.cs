namespace CardboardBox.Anime.Bot;

using ChatGPT;
using Commands.Nsfw;
using Database.Mapping;
using Database.Generation;
using Services;

public static class Extensions
{
	public static TData? Case<TKey, TData>(this TKey? item, params (TKey, TData?)[] pars)
	{
		return item.Case(default, pars);
	}

	public static TData Case<TKey, TData>(this TKey? item, TData def, params (TKey, TData)[] pars)
	{
		if (item == null) return def;

		foreach (var (key, val) in pars)
			if (key != null && key.Equals(val))
				return val;

		return def;
	}

	public static Color PlatformColor(this string platform)
	{
		return platform switch
		{
			"crunchyroll" => new Color(244, 117, 33),
			"vrvselect" => new Color(128, 130, 133),
			"mondo" => new Color(233, 55, 53),
			"hidive" => new Color(0, 174, 240),
			"funimation" => new Color(96, 48, 161),
			_ => Color.Gold
		};
	}

	public static Color PlatformColor(this DbAnime anime)
	{
		return anime.PlatformId.PlatformColor();
	}

	public static int IndexOf<T>(this IEnumerable<T> data, Func<T, bool> predicate)
	{
		int i = 0;
		foreach(var item in data)
		{
			if (predicate(item)) return i;
			i++;
		}

		return -1;
	}

	public static EmbedBuilder AddOptField(this EmbedBuilder bob, string title, string? data, bool inline = false)
	{
		if (string.IsNullOrEmpty(data)) return bob;

		return bob.AddField(title, data, inline);
	}

	public static Task UpdateDefered(this ComponentHandler handler, Action<MessageProperties> action)
	{
		return handler.Update(action);
	}

	public static Task Remove(this ComponentHandler handler, Action<MessageProperties>? action = null)
	{
		action ??= (t) => { };

		return handler.RemoveComponents(action);
	}

	public static int Martial(this int input, int max)
	{
		return (input + max) % max;
	}

	public static IServiceCollection AddDatabase(this IServiceCollection services)
	{
		MapConfig.AddMap(c =>
		{
			c.ForEntity<LookupRequest>()
			 .ForEntity<GptAuthorized>()
			 .ForEntity<NsfwConfig>();
		});

		MapConfig.StartMap();

		SqlMapper.RemoveTypeMap(typeof(DateTime));
		SqlMapper.RemoveTypeMap(typeof(DateTime?));
		SqlMapper.RemoveTypeMap(typeof(string[]));
		SqlMapper.RemoveTypeMap(typeof(bool));
		SqlMapper.RemoveTypeMap(typeof(bool?));
		SqlMapper.AddTypeHandler(new DateTimeHandler());
		SqlMapper.AddTypeHandler(new NullableDateTimeHandler());
		SqlMapper.AddTypeHandler(new DefaultJsonHandler<string[]>(() => Array.Empty<string>()));
		SqlMapper.AddTypeHandler(new BooleanHandler());
		SqlMapper.AddTypeHandler(new NullableBooleanHandler());

		return services
			.AddSingleton<ISqlService, SqliteService>()
			.AddTransient<IDbQueryBuilderService, SqliteDbQueryBuilderService>()
			.AddTransient<IDbService, DbService>()
			.AddTransient<ILookupDbService, LookupDbService>()
			.AddTransient<IGptDbService, GptDbService>()
			.AddTransient<INsfwConfigDbService, NsfwConfigDbService>()
			.AddTransient<IChatGptService, ChatGptService>()
			.AddTransient<INsfwService, NsfwService>();
	}
}
