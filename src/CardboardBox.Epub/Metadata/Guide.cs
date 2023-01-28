namespace CardboardBox.Epub.Metadata;

public class Guide : IXmlItem
{
	public string Type { get; set; }
	public string Title { get; set; }
	public string Href { get; set; }

	public Guide(string type, string title, string href)
	{
		Type = type;
		Title = title;
		Href = href;
	}

	public XElement ToElement()
	{
		return new XElement("reference")
			.AddAttribute("type", Type)
			.AddAttribute("title", Title)
			.AddAttribute("href", Href);
	}

	public override string ToString()
	{
		return $"<reference type=\"{Type}\" title=\"{Title}\" href=\"{Href}\"/>";
	}
}
