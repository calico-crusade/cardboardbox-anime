using Microsoft.Extensions.Configuration;

namespace CardboardBox.Anime
{
	public static class Extensions
	{
		public static T Bind<T>(this IConfiguration config, string? section = null)
		{
			var i = Activator.CreateInstance<T>();
			var target = string.IsNullOrEmpty(section) ? config : config.GetSection(section);
			target.Bind(i);
			return i;
		}
	}
}