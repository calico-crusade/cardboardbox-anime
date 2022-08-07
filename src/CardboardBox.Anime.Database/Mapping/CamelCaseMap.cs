using Dapper.FluentMap.Conventions;
using System.Text.RegularExpressions;

namespace CardboardBox.Anime.Database.Mapping
{
	public class CamelCaseMap : Convention
	{
		public CamelCaseMap()
		{
			Properties()
				.Configure(c =>
					c.Transform(s =>
						Regex.Replace(
							input: s,
							pattern: "([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])",
							replacement: "$1$3_$2$4"
						).ToLower()
					)
				);
		}
	}
}
