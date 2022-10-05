namespace CardboardBox.Epub.Metadata
{
	public class MetaData
	{
		public const string XMLNS = "http://purl.org/dc/elements/1.1/";

		public static XNamespace DcNs => "dc";

		public string Title { get; set; }
		public string Language { get; set; } = "en";
		public string Id { get; set; }
		public string? Publisher { get; set; } = "CardboardBox";
		public string? Rights { get; set; }
		public string? Cover { get; set; }
		public DateTime? Date { get; set; }
		public DateTime? DcTermsModified { get; set; }

		public List<Creator> Creators { get; set; } = new();

		public List<XElement> Extras { get; set; } = new();

		public MetaData(string title, string id)
		{
			Title = title;
			Id = id;
		}

		public override string ToString()
		{
			var dcterms = (DcTermsModified ?? DateTime.Now).ToString("u").Replace(" ", "T");

			var lines = new List<string>
			{
				$"<dc:title>{Title}</dc:title>",
				$"<dc:language>{Language}</dc:language>",
				$"<dc:identifier id=\"pub-id\">{Id}</dc:identifier>",
				$"<meta property=\"dcterms:modified\">{dcterms}</meta>"
			};

			if (!string.IsNullOrEmpty(Publisher)) lines.Add($"<dc:publisher>{Publisher}</dc:publisher>");
			if (!string.IsNullOrEmpty(Rights)) lines.Add($"<dc:rights>{Rights}</dc:rights>");
			if (!string.IsNullOrEmpty(Cover)) lines.Add($"<meta name=\"cover\" content=\"{Cover}\" />");
			if (Date != null) lines.Add($"<dc:date>{Date?.ToShortDateString()}</dc:date>");

			for (var i = 0; i < Creators.Count; i++)
			{
				var ci = $"creator{i:00}";
				var c = Creators[i];

				lines.Add($"<dc:creator id=\"{ci}\">{c.Name}</dc:creator>");
				lines.Add($"<meta refines=\"#{ci}\" property=\"file-as\">{c.FileAs}</meta>");
				lines.Add($"<meta refines=\"#{ci}\" scheme=\"marc:relators\" property=\"role\">{c.Role}</meta>");
				lines.Add($"<meta refines=\"#{ci}\" property=\"display-seq\">{i + 1}</meta>");
			}

			return "\r\n    " + string.Join("\r\n    ", lines);
		}
	}
}
