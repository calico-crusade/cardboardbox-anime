namespace CardboardBox.Epub;

public partial class EpubBuilder
{
	public string GenerateContainer()
	{
		return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
    <rootfiles>
        <rootfile full-path=""{ContentDirectory}/content.opf"" media-type=""application/oebps-package+xml""/>
   </rootfiles>
</container>";
	}

	public string GenerateContentOpf() => Content.ToString();

	public string GenerateNcx() => Ncx.Serialize().Replace("version=\"2005-1\"", "xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\"");

	public string GenerateMimetype() => "application/epub+zip";

	public string GenerateHtmlPage(string body, bool nomargins = false, string type = EPUB_TYPE_CHAPTER)
	{
		var bodyClass = nomargins ? $" class=\"{HTML_BODY_CLASS_NOMARGIN}\"" : "";

		var sheets = string.Join("\r\n\t", GlobalStylesheets.Select(t => $"<link href=\"{t}\" rel=\"stylesheet\" type=\"text/css\"/>"));

		return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE html>

<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"" lang=""en"" xml:lang=""en"">
<head>
  <meta content=""text/html; charset=UTF-8"" http-equiv=""default-style""/>
  <title>{Content.Meta.Title}</title>
  {sheets}
</head>

<body{bodyClass}>
  <section epub:type=""{type}"">
	{body}
  </section>
</body>
</html>";
	}

	public string GenerateToc()
	{
		var sheets = string.Join("\r\n\t", GlobalStylesheets.Select(t => $"<link href=\"{t}\" rel=\"stylesheet\" type=\"text/css\"/>"));

		var bob = new StringBuilder();
		foreach(var (name, src) in Ncx.Nav)
		{
			bob.AppendLine($@"      <li class=""toc-front""><a href=""../{src.Replace("\\", "/")}"">{name}</a></li>");
		}

		return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE html>

<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"" lang=""en"" xml:lang=""en"">
<head>
  <meta content=""text/html; charset=UTF-8"" http-equiv=""default-style""/>
  <title>{Content.Meta.Title}</title>
  {sheets}
</head>
<body>
  <nav epub:type=""toc"" id=""toc"">
    <h1 class=""toc-title"">Table of Contents</h1>
    <ol class=""none"" epub:type=""list"">
{bob}
    </ol>
  </nav>
</body>
</html>";
	}
}
