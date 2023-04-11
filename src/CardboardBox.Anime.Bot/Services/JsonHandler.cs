using System.Data;

namespace CardboardBox.Anime.Bot.Services;

/// <summary>
/// Handles mapping generics from string results
/// </summary>
/// <typeparam name="T">The type of value</typeparam>
public class JsonHandler<T> : SqlMapper.TypeHandler<T>
	where T : new()
{
	/// <summary>
	/// Parses the input value into a generice type
	/// </summary>
	/// <param name="value">The value to parse</param>
	/// <returns>The parsed generic array</returns>
	public override T Parse(object value)
	{
		if (value == null || value is not string str) return new();

		return JsonSerializer.Deserialize<T>(str) ?? new();
	}

	/// <summary>
	/// Sets the database parameter value to the proper representation for a generic array
	/// </summary>
	/// <param name="parameter">The database parameter to set</param>
	/// <param name="value">The value to set it to</param>
	public override void SetValue(IDbDataParameter parameter, T value)
	{
		if (value != null)
			parameter.Value = JsonSerializer.Serialize(value);
	}
}

/// <summary>
/// Handles mapping generics from string results
/// </summary>
/// <typeparam name="T">The type of value</typeparam>
public class NullableJsonHandler<T> : SqlMapper.TypeHandler<T?>
{
	/// <summary>
	/// Parses the input value into a generice type
	/// </summary>
	/// <param name="value">The value to parse</param>
	/// <returns>The parsed generic array</returns>
	public override T? Parse(object value)
	{
		if (value == null || value is not string str) return default;

		return JsonSerializer.Deserialize<T>(str) ?? default;
	}

	/// <summary>
	/// Sets the database parameter value to the proper representation for a generic array
	/// </summary>
	/// <param name="parameter">The database parameter to set</param>
	/// <param name="value">The value to set it to</param>
	public override void SetValue(IDbDataParameter parameter, T? value)
	{
		if (value != null)
			parameter.Value = JsonSerializer.Serialize(value);
	}
}

/// <summary>
/// Handles mapping generics from string results
/// </summary>
/// <typeparam name="T">The type of value</typeparam>
public class DefaultJsonHandler<T> : SqlMapper.TypeHandler<T>
{
	/// <summary>
	/// How to create a default for this type
	/// </summary>
	public Func<T> Default { get; }

	/// <summary>
	/// Handles mapping generics from string results
	/// </summary>
	/// <param name="default">How to create a default for this type</param>
	public DefaultJsonHandler(Func<T> @default)
	{
		Default = @default;
	}

	/// <summary>
	/// Parses the input value into a generice type
	/// </summary>
	/// <param name="value">The value to parse</param>
	/// <returns>The parsed generic array</returns>
	public override T Parse(object value)
	{
		if (value == null || value is not string str) return Default();

		return JsonSerializer.Deserialize<T>(str) ?? Default();
	}

	/// <summary>
	/// Sets the database parameter value to the proper representation for a generic array
	/// </summary>
	/// <param name="parameter">The database parameter to set</param>
	/// <param name="value">The value to set it to</param>
	public override void SetValue(IDbDataParameter parameter, T value)
	{
		parameter.Value = JsonSerializer.Serialize(value);
	}
}