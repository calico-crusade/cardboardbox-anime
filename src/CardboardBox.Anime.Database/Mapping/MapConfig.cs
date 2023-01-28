using Dapper.FluentMap;
using Dapper.FluentMap.Configuration;

namespace CardboardBox.Anime.Database.Mapping;

public static class MapConfig
{
	private static readonly List<Action<FluentConventionConfiguration>> _actions = new();

	public static void AddMap(Action<FluentConventionConfiguration> action)
	{
		_actions.Add(action);
	}

	public static void StartMap()
	{
		FluentMapper.Initialize(config =>
		{
			var conv = config
				.AddConvention<CamelCaseMap>();

			foreach (var action in _actions)
				action?.Invoke(conv);
		});
	}
}
