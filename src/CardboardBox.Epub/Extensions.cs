namespace CardboardBox.Epub;

public static class Extensions
{
	//
	public static XElement AddAttribute(this XElement el, XName name, object? value)
	{
		el.SetAttributeValue(name, value);
		return el;
	}
	//
	public static XElement AddAttributes(this XElement el, params (XName Attribute, object? Value)[] attributes)
	{
		foreach (var (atr, val) in attributes)
			el.AddAttribute(atr, val);
		return el;
	}
	//
	public static XElement AddElements(this XElement parent, params object?[] children)
	{
		parent.Add(children);
		return parent;
	}
	//
	public static XElement AddElement(this XElement parent, XName tag, object? value, params (XName Attribute, object? Value)[] attributes)
	{
		var el = new XElement(tag)
			.AddAttributes(attributes);

		if (value != null)
			el.AddElements(value);

		parent.AddElements(el);
		return parent;
	}
	//
	public static XElement AddOptElement(this XElement parent, XName tag, object? value, params (XName Attribute, object? Value)[] attributes)
	{
		if (value == null) return parent;

		return parent.AddElement(tag, value, attributes);
	}

	public static string Serialize(this IXmlItem item)
	{
		var doc = new XDocument
		{
			Declaration = new XDeclaration("1.0", "utf-8", null)
		};
		var el = item.ToElement();
		doc.Add(el);

		using var writer = new Utf8StringWriter();
		doc.Save(writer);

		return writer.ToString();
	}

	//
	public static Stream ToStream(this string content)
	{
		var bytes = Encoding.UTF8.GetBytes(content);
		return ToStream(bytes);
	}
	//
	public static Stream ToStream(this byte[] content)
	{
		return new MemoryStream(content);
	}

	//
	public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> collection, int count = 1)
	{
		var ar = collection.ToArray();
		var ac = ar.Length - count;
		if (ac <= 0) yield break;

		for(var i = 0; i < ac; i++)
			yield return ar[i];
	}

	//
	public static string PurgePathChars(this string text)
	{
		var chars = Path.GetInvalidFileNameChars()
			.Union(Path.GetInvalidPathChars())
			.ToArray();

		foreach (var c in chars)
			text = text.Replace(c.ToString(), "");

		return text;
	}

	public static string SnakeName(this string text)
	{
		var ar = text
			.PurgePathChars()
			.ToLower()
			.ToCharArray()
			.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-')
			.ToArray();
		var cur = new string(ar).Replace(" ", "-").ToLower();

		while (cur.Contains("--"))
			cur = cur.Replace("--", "-");

		return cur;
	}

	public static string FixFileName(this string text)
	{
		var ext = Path.GetExtension(text).TrimStart('.');
		var name = Path.GetFileNameWithoutExtension(text).TrimEnd('.').SnakeName();

		return $"{name.Replace("-", "")}.{ext}";
	}

	public static string CleanId(this string text)
	{
		if (text.Length == 0) return text;

		if (char.IsDigit(text[0]))
			return "a" + text;

		return text;
	}
}
