using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System.Numerics;

namespace CardboardBox.Anime.Cli.Verbs;

using LightNovel.Core;

[Verb("generate-covers", HelpText = "Generates cover images for book series")]
public class GenerateCoversOptions
{
	private const string FONT_FAMILY = "Anime Ace 2.0 BB";
	private const int FONT_SIZE = 96;
	private const string FONT_COLOR = "#000";
	private const string FONT_OUTLINE_COLOR = "#fff";
	private const int MARGIN = 50;
	private const int PEN_WIDTH = 5;
	private const Position WHERE = Position.Bottom | Position.Center;
	private const string TEXT = "#{0}";

	[Option('i', "series-id", HelpText = "The ID of the light novel series to generate covers for")]
	public long? SeriesId { get; set; }

	[Option('o', "output", HelpText = "The output directory path for the generated covers")]
	public string? Output { get; set; }

	[Option('s', "font-size", HelpText = "The font size of the book number", Default = FONT_SIZE)]
	public int FontSize { get; set; } = FONT_SIZE;

	[Option('f', "font", HelpText = "The font family for the text", Default = FONT_FAMILY)]
	public string Font { get; set; } = FONT_FAMILY;

	[Option('c', "font-color", HelpText = "The font color of the text", Default = FONT_COLOR)]
	public string FontColor { get; set; } = FONT_COLOR;

	[Option('p', "font-color-pen", HelpText = "The background color of the text", Default = FONT_OUTLINE_COLOR)]
	public string FontColorPen { get; set; } = FONT_OUTLINE_COLOR;

	[Option('m', "margin", HelpText = "The margin from the edge of the cover for the text", Default = MARGIN)]
	public int Margin { get; set; } = MARGIN;

	[Option("font-pen-width", HelpText = "The width of the pen to use for the background color", Default = PEN_WIDTH)]
	public int PenWidth { get; set; } = PEN_WIDTH;

	[Option('w', "where", HelpText = "Where to place the text on the cover (Top, Middle, Bottom, Left, Right, Center)", Default = WHERE)]
	public Position Where { get; set; } = WHERE;

	[Option('t', "text", HelpText = "The text format for the book number, {0} will be replaced with the book number", Default = TEXT)]
	public string Text { get; set; } = TEXT;
}

[Flags]
public enum Position
{
	Top = 1 << 0,
	Middle = 1 << 1,
	Bottom = 1 << 2,

	Left = 1 << 3,
	Right = 1 << 4,
	Center = 1 << 5
}

internal class GenerateCoversVerb(
    ILogger<GenerateCoversVerb> logger,
    ILnDbService _db,
	IApiService _api) : BooleanVerb<GenerateCoversOptions>(logger)
{
	public async Task<(string? url, Book[] books)> CoverUrl(long seriesId)
	{
		var scaffold = await _db.Series.PartialScaffold(seriesId);
		if (scaffold is null) return (null, []);

		var url = scaffold.Series.Image;
		var books = scaffold.Books.Select(t => t.Book).ToArray();
		return (url, books);
	}

	public static (VerticalAlignment v, HorizontalAlignment h, TextAlignment t, Vector2 o) DetermineAlignment(GenerateCoversOptions options, Rectangle rectangle)
	{
		bool top = options.Where.HasFlag(Position.Top),
			 middle = options.Where.HasFlag(Position.Middle),
			 bottom = !top && !middle,
			 left = options.Where.HasFlag(Position.Left),
			 center = options.Where.HasFlag(Position.Center),
			 right = !left && !center;

		var v = top ? VerticalAlignment.Top :
				middle ? VerticalAlignment.Center :
				VerticalAlignment.Bottom;
		var h = left ? HorizontalAlignment.Left :
				center ? HorizontalAlignment.Center :
				HorizontalAlignment.Right;
		var t = left ? TextAlignment.Start :
				center ? TextAlignment.Center :
				TextAlignment.End;

		Vector2 o = top && left ? new(rectangle.Left, rectangle.Top) :
				top && center ? new(rectangle.Left + rectangle.Width / 2, rectangle.Top) :
				top && right ? new(rectangle.Right, rectangle.Top) :
				middle && left ? new(rectangle.Left, rectangle.Top + rectangle.Height / 2) :
				middle && center ? new(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2) :
				middle && right ? new(rectangle.Right, rectangle.Top + rectangle.Height / 2) :
				bottom && left ? new(rectangle.Left, rectangle.Bottom) :
				bottom && center ? new(rectangle.Left + rectangle.Width / 2, rectangle.Bottom) :
				new(rectangle.Right, rectangle.Bottom);

		return (v, h, t, o);
	}

    public override async Task<bool> Execute(GenerateCoversOptions options, CancellationToken token)
    {
		var seriesId = options.SeriesId;
		var output = options.Output;

		if (seriesId == null)
		{
			_logger.LogWarning("Please specify the series ID or book ID");
			return false;
		}

		if (string.IsNullOrEmpty(output))
			output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "novels", "covers");

		output = Path.Combine(output, seriesId.Value.ToString());

		if (!Directory.Exists(output))
			Directory.CreateDirectory(output);

		var (cover, books) = await CoverUrl(seriesId.Value);
		if (string.IsNullOrEmpty(cover))
		{
			_logger.LogWarning("Could not find a cover image for the specified series/book");
			return false;
		}

		if (books.Length <= 0) 
		{
			_logger.LogWarning("No books found for the specified series");
			return false;
		}

		if (!SystemFonts.TryGet(options.Font, out var fontFamily))
		{
			_logger.LogWarning("Could not find font: {name}", options.Font);
			return false;
		}

		if (!Color.TryParse(options.FontColor, out var color))
		{
			_logger.LogWarning("Could not parse the color: {color}", options.FontColor);
			return false;
		}

		if (!Color.TryParse(options.FontColorPen, out var colorPen))
		{
			_logger.LogWarning("Could not parse the pen color: {color}", options.FontColorPen);
			return false;

		}

		var font = fontFamily.CreateFont(options.FontSize, FontStyle.Bold);
		var brush = new SolidBrush(color);
		var pen = new SolidPen(colorPen, options.PenWidth);

		var (stream, _, name, _) = await _api.GetData(cover);
		using var io = new MemoryStream();
		await stream.CopyToAsync(io, token);
		await stream.DisposeAsync();

		name = name.Replace("\"", string.Empty);
		var ext = Path.GetExtension(name).Trim('.');
		var fileName = Path.GetFileNameWithoutExtension(name);

		for (var i = 0; i < books.Length; i++)
		{
			var book = books[i];
			var fn = $"{fileName}-{i + 1}.{ext}";
			var outputPath = Path.Combine(output, fn);
			io.Position = 0;
			using var image = await Image.LoadAsync(io, token);
			var text = string.Format(options.Text, i + 1);
			var rect = new Rectangle(options.Margin, options.Margin,
				image.Width - (options.Margin * 2), image.Height - (options.Margin * 2));
			var (v, h, t, o) = DetermineAlignment(options, rect);
			var opts = new RichTextOptions(font)
			{
				HorizontalAlignment = h,
				VerticalAlignment = v,
				TextAlignment = t,
				WordBreaking = WordBreaking.Standard,
				WrappingLength = rect.Width,
				Origin = o
			};

			image.Mutate(t => t.DrawText(opts, text, brush, pen));
			await image.SaveAsPngAsync(outputPath, token);
			_logger.LogInformation("Written image: {path}", outputPath);

			var url = $"https://static.index-0.com/image/novels/{seriesId}/{fn}";

			if (!book.Inserts.Contains(book.CoverImage, StringComparer.InvariantCultureIgnoreCase) &&
				!book.Inserts.Contains(url, StringComparer.InvariantCultureIgnoreCase) &&
				!string.IsNullOrWhiteSpace(book.CoverImage))
				book.Inserts = [.. book.Inserts, book.CoverImage];

			book.CoverImage = url;
			await _db.Books.Update(book);
			_logger.LogInformation("Book updated with cover: {cover}", book.CoverImage);
		}

		_logger.LogInformation("Finished >> {output}", output);
		return true;
	}
}
