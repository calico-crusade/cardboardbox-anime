namespace CardboardBox.Epub.Metadata;

public class Ncx : IXmlItem
{
	public const string XMLNS = "http://www.daisy.org/z3986/2005/ncx/";
	public const string VERSION = "2005-1";

	public string Id { get; set; }
	public string Title { get; set; }

	public List<(string Name, string Src)> Nav { get; set; } = new();

	public Ncx(string title, string id)
	{
		Title = title;
		Id = id;
	}

	public XElement ToElement()
	{
		var el = new XElement("ncx")
			.AddAttribute("version", VERSION);

		//Add Header
		el.AddElements(new XElement("head")
			.AddElement("meta", null, ("name", "dtb:uid"), ("content", Id))
			.AddElement("meta", null, ("name", "dtb:depth"), ("content", "1"))
			.AddElement("meta", null, ("name", "dtb:totalPageCount"), ("content", "0"))
			.AddElement("meta", null, ("name", "dtb:maxPageNumber"), ("content", "0")));

		//Add Title
		el.AddElements(new XElement("docTitle").AddElement("text", Title));

		//Add NavMap

		var map = new XElement("navMap");
		for(var i = 0; i < Nav.Count; i++)
		{
			var (name, src) = Nav[i];
			var id = $"navPoint" + (i + 1);
			var point = new XElement("navPoint")
				.AddAttribute("id", id)
				.AddElements(new XElement("navLabel").AddElement("text", name))
				.AddElement("content", null, ("src", src.Replace("\\", "/")));

			map.AddElements(point);
		}
		el.AddElements(map);

		return el;
	}
}
