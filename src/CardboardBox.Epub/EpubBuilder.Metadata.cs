namespace CardboardBox.Epub
{
	using Metadata;

	public interface IEpubBuilderMetadata
	{
		IEpubBuilder Language(string language);

		IEpubBuilder Publisher(string publisher);

		IEpubBuilder Rights(string rights);

		IEpubBuilder Date(DateTime date);

		IEpubBuilder AddCreator(string name, string fileAs, string role);

		IEpubBuilder Author(string name, string? fileAs = null);

		IEpubBuilder Illustrator(string name, string? fileAs = null);

		IEpubBuilder Translator(string name, string? fileAs = null);

		IEpubBuilder Editor(string name, string? fileAs = null);

		IEpubBuilder Metadata(XElement element);

		IEpubBuilder BelongsTo(string title, int position, string type = MetaData.COLLECTION_TYPE_SET);
	}

	public partial class EpubBuilder
	{
		public IEpubBuilder Author(string name, string? fileAs = null)
		{
			return AddCreator(name, fileAs ?? GenerateFileAs(name), Creator.ROLE_AUTHOR);
		}

		public IEpubBuilder Editor(string name, string? fileAs = null)
		{
			return AddCreator(name, fileAs ?? GenerateFileAs(name), Creator.ROLE_EDITOR);
		}

		public IEpubBuilder Illustrator(string name, string? fileAs = null)
		{
			return AddCreator(name, fileAs ?? GenerateFileAs(name), Creator.ROLE_ILLIUSTRATOR);
		}

		public IEpubBuilder Translator(string name, string? fileAs = null)
		{
			return AddCreator(name, fileAs ?? GenerateFileAs(name), Creator.ROLE_TRANSLATOR);
		}

		public string GenerateFileAs(string name)
		{
			var parts = name.ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length <= 1) return name;

			var last = parts.Last();
			var rest = string.Join(" ", parts.SkipLast());

			return $"{last}, {rest}";
		}

		public IEpubBuilder AddCreator(string name, string fileAs, string role)
		{
			Content.Meta.Creators.Add(new Creator(name, fileAs, role));
			return this;
		}

		public IEpubBuilder Date(DateTime date)
		{
			Content.Meta.Date = date;
			return this;
		}

		public IEpubBuilder Language(string language)
		{
			Content.Meta.Language = language;
			return this;
		}

		public IEpubBuilder Publisher(string publisher)
		{
			Content.Meta.Publisher = publisher;
			return this;
		}

		public IEpubBuilder Rights(string rights)
		{
			Content.Meta.Rights = rights;
			return this;
		}

		public IEpubBuilder Metadata(XElement element)
		{
			Content.Meta.Extras.Add(element);
			return this;
		}

		public IEpubBuilder BelongsTo(string title, int position, string type = MetaData.COLLECTION_TYPE_SET)
		{
			Content.Meta.CollectionTitle = title;
			Content.Meta.CollectionType = type;
			Content.Meta.CollectionPosition = position;
			return this;
		}
	}
}
