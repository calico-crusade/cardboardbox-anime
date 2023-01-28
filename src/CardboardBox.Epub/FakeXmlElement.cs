namespace CardboardBox.Epub;

public class FakeXmlElement
{
	public string Tag { get; }
	public List<(string Attribute, string Value)> Attributes { get; } = new();
	public string? Content { get; }

	public FakeXmlElement(string tag, string? content)
	{
		Tag = tag;
		Content = content;
	}

	public FakeXmlElement Attribute(string attr, string val)
	{
		Attributes.Add((attr, val));
		return this;
	}

	public override string ToString()
	{
		var attrs = string.Join(" ", Attributes.Select(t => $"{t.Attribute}=\"{t.Value}\""));

		if (!string.IsNullOrWhiteSpace(attrs))
			attrs += " ";

		if (string.IsNullOrWhiteSpace(Content))
			return $"<{Tag} {attrs}/>";

		return $"<{Tag} {attrs}>{Content}</{Tag}>";
	}
}
