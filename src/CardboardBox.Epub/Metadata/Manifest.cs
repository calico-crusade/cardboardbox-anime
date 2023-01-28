namespace CardboardBox.Epub.Metadata;

public class Manifest
{
	public string TocNcx { get; set; }

	public List<ManifestItem> Items { get; set; } = new();

	public Manifest(string tocNcx)
	{
		TocNcx = tocNcx;
	}

	public Manifest Add(string id, string path, string type, string? props = null)
	{
		Items.Add(new ManifestItem(id, path, type, props));
		return this;
	}

	public Manifest AddStylesheet(string id, string path, string? props = null)
	{
		return Add(id, path, ManifestItem.MEDIA_TYPE_CSS, props);
	}

	public Manifest AddImage(string id, string path, string? props = null)
	{
		var ext = Path.GetExtension(path).TrimStart('.').ToLower();
		var type = ext switch
		{
			"jpg" => ManifestItem.MEDIA_TYPE_JPEG,
			"jpeg" => ManifestItem.MEDIA_TYPE_JPEG,
			"png" => ManifestItem.MEDIA_TYPE_PNG,
			"webp" => ManifestItem.MEDIA_TYPE_WEBP,
			"gif" => ManifestItem.MEDIA_TYPE_GIF,
			_ => throw new NotSupportedException($"\"{ext}\" is not a valid image extension!")
		};

		return Add(id, path, type, props);
	}

	public Manifest AddPage(string id, string path, string? props = null)
	{
		return Add(id, path, ManifestItem.MEDIA_TYPE_XHTML, props);
	}

	public override string ToString()
	{
		var bob = new StringBuilder();

		bob.AppendLine("<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\"/>");

		foreach(var item in Items)
		{
			var fe = new FakeXmlElement("item", null)
				.Attribute("id", item.Id.CleanId())
				.Attribute("media-type", item.MediaType)
				.Attribute("href", item.Href.Replace("\\", "/"));

			if (!string.IsNullOrWhiteSpace(item.Properties))
				fe.Attribute("properties", item.Properties);

			bob.AppendLine("    " + fe.ToString());
		}

		return bob.ToString();
	}
}
