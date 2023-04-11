using System.Data;

namespace CardboardBox.Anime.Bot.Services;

/// <summary>
/// Handles mapping <see cref="bool"/> results from string results.
/// This a polyfill to add proper <see cref="bool"/> handling to engines like SQLite
/// </summary>
public class BooleanHandler : SqlMapper.TypeHandler<bool>
{
	/// <summary>
	/// Parses the input value into a proper <see cref="bool"/>
	/// </summary>
	/// <param name="value">The value to parse</param>
	/// <returns>The parsed <see cref="bool"/></returns>
	public override bool Parse(object value)
	{
		return value switch
		{
			string str => bool.TryParse(str, out var res) ? res : false,
			int val => val == 1,
			long val => val == 1,
			float val => val == 1,
			_ => false
		};
	}

	/// <summary>
	/// Sets the database parameter value to the proper representation for a <see cref="bool"/>
	/// </summary>
	/// <param name="parameter">The database parameter to set</param>
	/// <param name="value">The value to set it to</param>
	public override void SetValue(IDbDataParameter parameter, bool value)
	{
		parameter.Value = value ? 1 : 0;
	}
}

/// <summary>
/// Handles mapping nullable <see cref="bool"/> results from integer or string results.
/// This a polyfill to add proper nullable <see cref="bool"/> handling to engines like SQLite
/// </summary>
public class NullableBooleanHandler : SqlMapper.TypeHandler<bool?>
{
	/// <summary>
	/// Parses the input value into a proper nullable <see cref="bool"/>
	/// </summary>
	/// <param name="value">The value to parse</param>
	/// <returns>The parsed nullable <see cref="bool"/></returns>
	public override bool? Parse(object value)
	{
		if (value == null) return null;

		return value switch
		{
			string str => bool.TryParse(str, out var res) ? res : null,
			int val => val == 1,
			long val => val == 1,
			float val => val == 1,
			_ => null
		};
	}

	/// <summary>
	/// Sets the database parameter value to the proper representation for a nullable <see cref="bool"/>
	/// </summary>
	/// <param name="parameter">The database parameter to set</param>
	/// <param name="value">The value to set it to</param>
	public override void SetValue(IDbDataParameter parameter, bool? value)
	{
		parameter.Value = value == null ? null : (value.Value ? 1 : 0);
	}
}

