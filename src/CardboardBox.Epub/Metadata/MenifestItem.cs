namespace CardboardBox.Epub.Metadata
{
	public class ManifestItem : IXmlItem
	{
		public const string MEDIA_TYPE_CSS = "text/css";
		public const string MEDIA_TYPE_XHTML = "application/xhtml+xml";
		public const string MEDIA_TYPE_JPEG = "image/jpeg";
		public const string MEDIA_TYPE_NCX = "application/x-dtbncx+xml";
		public const string MEDIA_TYPE_PNG = "image/png";
		public const string MEDIA_TYPE_GIF = "image/gif";
		public const string MEDIA_TYPE_WEBP = "image/webp";

		public string Id { get; set; }
		public string Href { get; set; }
		public string MediaType { get; set; }

		public string? Properties { get; set; }

		public ManifestItem(string id, string href, string mediaType, string? props = null)
		{
			Id = id;
			Href = href;
			MediaType = mediaType;
			Properties = props;
		}

		public XElement ToElement()
		{
			var el = new XElement("item")
				.AddAttribute("id", Id)
				.AddAttribute("href", Href)
				.AddAttribute("media-type", MediaType);

			if (!string.IsNullOrEmpty(Properties))
				el.AddAttribute("properties", Properties);

			return el;
		}
	}
}
