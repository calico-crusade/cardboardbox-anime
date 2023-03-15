namespace CardboardBox.Anime.Bot;

public class GptAuthorized : DbObject
{
	public string? UserId { get; set; }

	public string? ServerId { get; set; }

	public string Type { get; set; } = WHITE_LIST;

	public const string WHITE_LIST = "white-list";
	public const string BLACK_LIST = "black-list";
}
