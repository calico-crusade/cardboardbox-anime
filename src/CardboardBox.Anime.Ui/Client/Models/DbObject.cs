namespace CardboardBox.Anime.Ui.Client.Models;

public abstract class DbObject
{
	public long Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public DateTime? DeletedAt { get; set; }
}
