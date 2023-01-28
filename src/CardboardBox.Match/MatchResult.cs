using System.Text.Json.Serialization;

namespace CardboardBox.Match;

public class MatchResult
{
	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	[JsonPropertyName("error")]
	public string[] Error { get; set; } = Array.Empty<string>();

	[JsonPropertyName("method")]
	public string Method { get; set; } = string.Empty;

	[JsonIgnore]
	public bool Success => Status == "ok";
}

public class MatchResult<T> : MatchResult
{
	[JsonPropertyName("result")]
	public T[] Result { get; set; } = Array.Empty<T>();
}

public class MatchSearchResults : MatchResult<MatchImage> { }

public class MatchSearchResults<T> : MatchResult<MatchMeta<T>> { }

public class MatchCompareResults : MatchResult<MatchScore> { }

public class MatchScore
{
	[JsonPropertyName("score")]
	public float Score { get; set; }
}

public class MatchImage : MatchScore
{
	[JsonPropertyName("filepath")]
	public string FilePath { get; set; } = string.Empty;
}

public class MatchMeta<T> : MatchImage
{
	[JsonPropertyName("metadata")]
	public T? Metadata { get; set; }
}
