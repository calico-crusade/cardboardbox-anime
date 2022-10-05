namespace CardboardBox.Epub.Metadata
{
	public class ContentOpf
	{
		public const string XMLNS = "http://www.idpf.org/2007/opf";

		public string Version { get; set; } = "3.0";

		public MetaData Meta { get; set; }

		public Manifest Manifest { get; set; }

		public List<string> SpineReferences { get; set; } = new();

		public List<Guide> Guide { get; set; } = new();
		
		public ContentOpf(MetaData meta, string tocNcx = "toc.ncx")
		{
			Meta = meta;
			Manifest = new Manifest(tocNcx);
		}

		public override string ToString()
		{
			var guide = string.Join("\r\n    ", Guide.Select(t => t.ToString()));
			var spine = string.Join("\r\n    ", SpineReferences.Select(t => $"<itemref idref=\"{t.CleanId()}\" />"));

			if (!string.IsNullOrWhiteSpace(guide))
				guide = $"\r\n  <guide>\r\n    {guide}\r\n  </guide>";

			return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<package version=""3.0"" unique-identifier=""pub-id"" xmlns=""http://www.idpf.org/2007/opf"" xml:lang=""en"">
  <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
    {Meta}
  </metadata>
  <manifest>
    {Manifest}
  </manifest>
  <spine page-progression-direction=""ltr"" toc=""ncx"">
    {spine}
  </spine>{guide}
</package>";
		}
	}
}
