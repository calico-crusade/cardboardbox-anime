namespace CardboardBox.Anime.Funimation;

public abstract class Filter
{
	public abstract string Key { get; }

	public virtual string? Value { get; set; }
}

public class Genre : Filter
{
	public override string Key => "genre";
	public Genre(string value) {  Value = value; }
}

public class Version : Filter
{
	public override string Key => "version";
	public Version(string value) { Value = value; }

	public static Version Simulcast => new("Simulcast");
	public static Version Uncut => new("Uncut");
}

public class Language : Filter
{
	public override string Key => "language";
	public Language(string value) { Value = value; }
}

public class Rating : Filter
{
	public override string Key => "ratingPairs";
	public Rating(params string[] values) { Value = string.Join("|", values); }
}

public class AnyFilter : Filter
{
	public override string Key { get; }

	public AnyFilter(string key, string? value)
	{
		Key = key;
		Value = value;
	}
}
