using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Vrv;

public class VrvLoadRequest
{
	[JsonPropertyName("policy")]
	public string Policy { get; set; } = "";

	[JsonPropertyName("signature")]
	public string Signature { get; set; } = "";

	[JsonPropertyName("keyPairId")]
	public string KeyPairId { get; set; } = "";

	public Dictionary<string, string> ToDictionary()
	{
		return new()
		{
			["Policy"] = Policy,
			["Signature"] = Signature,
			["Key-Pair-Id"] = KeyPairId
		};
	}
}
