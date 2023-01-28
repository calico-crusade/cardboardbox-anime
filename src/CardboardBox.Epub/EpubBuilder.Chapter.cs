namespace CardboardBox.Epub;

public interface IEpubBuilderChapters
{
	Task AddChapter(string title, Func<IChapterBuilder, Task> builder);
}

public partial class EpubBuilder
{
	public async Task AddChapter(string title, Func<IChapterBuilder, Task> builder)
	{
		var bob = new ChapterBuilder(title, this);
		await builder(bob);
	}
}
