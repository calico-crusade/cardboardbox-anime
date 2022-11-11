namespace CardboardBox.Anime.Bot
{
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
	}
}
