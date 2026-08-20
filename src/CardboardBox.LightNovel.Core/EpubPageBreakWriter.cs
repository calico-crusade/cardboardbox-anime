
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using CardboardBox.Epub;
using HtmlAgilityPack;

namespace CardboardBox.LightNovel.Core;


public static class EpubPageBreakWriter
{
    public static void AddApproximatePageBreaks(
        string inputEpubPath,
        string outputEpubPath,
        int wordsPerPage = 350)
    {
        if (wordsPerPage <= 0)
            throw new ArgumentOutOfRangeException(nameof(wordsPerPage));

        var files = ReadZipFiles(inputEpubPath);

        var opfPath = GetOpfPath(files);
        var opfDirectory = GetDirectory(opfPath);

        var opf = XDocument.Parse(Encoding.UTF8.GetString(files[opfPath]));

        var manifest = opf
            .Descendants()
            .Where(e => e.Name.LocalName == "item")
            .ToDictionary(
                e => (string?)e.Attribute("id") ?? "",
                e => new ManifestItem(
                    Id: (string?)e.Attribute("id") ?? "",
                    Href: (string?)e.Attribute("href") ?? "",
                    MediaType: (string?)e.Attribute("media-type") ?? "",
                    Properties: (string?)e.Attribute("properties") ?? ""));

        var spineIds = opf
            .Descendants()
            .Where(e => e.Name.LocalName == "itemref")
            .Select(e => (string?)e.Attribute("idref"))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray()!;

        var navItem = manifest.Values.FirstOrDefault(i =>
            i.Properties.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(p => p.Equals("nav", StringComparison.OrdinalIgnoreCase)));

        if (navItem is null)
            throw new InvalidOperationException("Could not find EPUB 3 nav document. Expected manifest item with properties=\"nav\".");

        var navPath = NormalizeZipPath(CombineZipPath(opfDirectory, navItem.Href));
        var navDirectory = GetDirectory(navPath);

        var pageBreaks = new EpubPageBreakGenerator(wordsPerPage);

        foreach (var spineId in spineIds)
        {
            if (!manifest.TryGetValue(spineId, out var item))
                continue;

            if (!IsXhtmlFile(item))
                continue;

            var chapterPath = NormalizeZipPath(CombineZipPath(opfDirectory, item.Href));

            if (!files.TryGetValue(chapterPath, out var chapterBytes))
                continue;

            var chapterHtml = Encoding.UTF8.GetString(chapterBytes);

            var hrefFromNavToChapter = MakeRelativeHref(navDirectory, chapterPath);
            var updated = pageBreaks.AddToXhtmlPage(chapterHtml, hrefFromNavToChapter);

            files[chapterPath] = Encoding.UTF8.GetBytes(updated);
        }

        if (pageBreaks.PageBreaks.Count == 0)
            throw new InvalidOperationException("No page breaks were generated. Could not find usable XHTML spine content.");

        files[navPath] = Encoding.UTF8.GetBytes(
            AddPageListToNav(
                Encoding.UTF8.GetString(files[navPath]),
                pageBreaks.PageBreaks));

        WriteZipFiles(outputEpubPath, files);
    }

    private static string AddPageListToNav(string navHtml, IReadOnlyList<EpubPageBreak> pages)
    {
        var doc = new HtmlDocument
        {
            OptionFixNestedTags = true,
            OptionAutoCloseOnEnd = true,
            OptionWriteEmptyNodes = false,
            OptionOutputAsXml = true,
        };

        doc.LoadHtml(navHtml);

        EnsureEpubNamespace(doc);

        var body = doc.DocumentNode.SelectSingleNode("//*[local-name()='body']");
        if (body is null)
            throw new InvalidOperationException("Nav document does not contain a body element.");

        var existingPageLists = body
            .Descendants()
            .Where(n =>
                n.NodeType == HtmlNodeType.Element &&
                n.Name.Equals("nav", StringComparison.OrdinalIgnoreCase) &&
                HasEpubType(n, "page-list"))
            .ToArray();

        foreach (var existing in existingPageLists)
            existing.Remove();

        var nav = HtmlNode.CreateNode(
            """
			<nav epub:type="page-list" hidden="hidden">
				<h2>Pages</h2>
				<ol></ol>
			</nav>
			""");

        var ol = nav.SelectSingleNode(".//ol")
            ?? throw new InvalidOperationException("Generated page-list nav is missing its ol element.");

        foreach (var page in pages)
        {
            var li = HtmlNode.CreateNode(
                $"""<li><a href="{HtmlEntity.Entitize(page.Href)}">{page.PageNumber}</a></li>""");

            ol.AppendChild(li);
        }

        body.AppendChild(nav);

        return doc.DocumentNode.OuterHtml;
    }

    private static bool HasEpubType(HtmlNode node, string type)
    {
        var value = node.GetAttributeValue("epub:type", "");
        return value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(v => v.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureEpubNamespace(HtmlDocument doc)
    {
        var html = doc.DocumentNode.SelectSingleNode("//*[local-name()='html']");
        if (html is null)
            return;

        var existing = html.GetAttributeValue("xmlns:epub", "");
        if (string.IsNullOrWhiteSpace(existing))
            html.SetAttributeValue("xmlns:epub", "http://www.idpf.org/2007/ops");
    }

    private static bool IsXhtmlFile(ManifestItem item)
    {
        if (item.MediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            return true;

        return item.Href.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
               item.Href.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, byte[]> ReadZipFiles(string epubPath)
    {
        using var archive = ZipFile.OpenRead(epubPath);

        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            using var stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            files[NormalizeZipPath(entry.FullName)] = memory.ToArray();
        }

        return files;
    }

    private static void WriteZipFiles(string epubPath, Dictionary<string, byte[]> files)
    {
        if (File.Exists(epubPath))
            File.Delete(epubPath);

        using var archive = ZipFile.Open(epubPath, ZipArchiveMode.Create);

        // EPUB requires mimetype to be first and uncompressed.
        if (files.TryGetValue("mimetype", out var mimetypeBytes))
        {
            var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using var stream = mimetypeEntry.Open();
            stream.Write(mimetypeBytes);
        }

        foreach (var file in files.OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (file.Key.Equals("mimetype", StringComparison.OrdinalIgnoreCase))
                continue;

            var entry = archive.CreateEntry(file.Key, CompressionLevel.Optimal);
            using var stream = entry.Open();
            stream.Write(file.Value);
        }
    }

    private static string GetOpfPath(Dictionary<string, byte[]> files)
    {
        if (!files.TryGetValue("META-INF/container.xml", out var containerBytes))
            throw new InvalidOperationException("EPUB is missing META-INF/container.xml.");

        var container = XDocument.Parse(Encoding.UTF8.GetString(containerBytes));

        var opfPath = container
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "rootfile")
            ?.Attribute("full-path")
            ?.Value;

        if (string.IsNullOrWhiteSpace(opfPath))
            throw new InvalidOperationException("Could not find OPF rootfile path in META-INF/container.xml.");

        return NormalizeZipPath(opfPath);
    }

    private static string GetDirectory(string path)
    {
        path = NormalizeZipPath(path);

        var index = path.LastIndexOf('/');
        if (index < 0)
            return "";

        return path[..index];
    }

    private static string CombineZipPath(string directory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return relativePath;

        return $"{directory.TrimEnd('/')}/{relativePath}";
    }

    private static string NormalizeZipPath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static string MakeRelativeHref(string fromDirectory, string toPath)
    {
        fromDirectory = NormalizeZipPath(fromDirectory);
        toPath = NormalizeZipPath(toPath);

        var fromUri = new Uri("zip://book/" + (string.IsNullOrEmpty(fromDirectory) ? "" : fromDirectory.TrimEnd('/') + "/"));
        var toUri = new Uri("zip://book/" + toPath);

        return Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());
    }

    private sealed record ManifestItem(
        string Id,
        string Href,
        string MediaType,
        string Properties);

}
