using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardboardBox.Anime.Cli
{
	using Anime.Database;
	using Epub;
	using LightNovel.Core;

	public interface IJmService
	{
		Task Run();
	}

	public class JmService : IJmService
	{
		private const string JM_IMG_DIR = @"C:\Users\Cardboard\Desktop\JM";

		private readonly IChapterDbService _db;
		private readonly ILogger _logger;

		public JmService(
			IChapterDbService db, 
			ILogger<JmService> logger)
		{
			_db = db;
			_logger = logger;
		}

		public IEnumerable<DbChapter[]> Chunks(DbChapter[] chaps, (int start, int stop)[] ranges)
		{
			int r = 0;
			var cur = new List<DbChapter>();

			for(var i = 0; i < chaps.Length; i++)
			{
				if (r >= ranges.Length) yield break;

				var (start, stop) = ranges[r];
				if (i + 1 < start || i + 1 > stop)
				{
					r++;
					yield return cur.ToArray();
					cur.Clear();
				}

				cur.Add(chaps[i]);
			}

			if (cur.Count > 0)
				yield return cur.ToArray();
		}

		public string CoverImage(int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Cover.jpg";
		public string ContentsImage(int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Contents.jpg";
		public string Inserts(int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Inserts";
		public string Forwards(int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Forwards";

		public async Task Run()
		{
			var (_, chaps) = await _db.Chapters("445C5E7AC91435D2155BC1D1DAAE8EB8", 1, 1000);
			var ranges = new[]
			{
				(1, 42),
				(43, 88),
				(89, 122),
				(123, 158),
				(159, 185),
				(186, 224),
				(225, 266),
				(267, 290),
				(291, 324),
				(325, 358),
				(359, 391),
				(391, 422)
			};

			var chunks = Chunks(chaps, ranges).ToArray();

			var tasks = new List<Task>();
			for (var i = 0; i < chunks.Length; i++)
				tasks.Add(Volume(chunks[i], i + 1));

			await Task.WhenAll(tasks.ToArray());
		}

		public async Task Volume(DbChapter[] chaps, int vol)
		{
			var title = chaps[0].Book;
			var path = $"{title.SnakeName()}-vol-{vol}.epub";

			_logger.LogInformation($"Starting processing of vol {vol} of {title} - {path}");

			using (var io = File.Create(path))
			await using (var epub = EpubBuilder.Create(title, io))
			{
				var bob = await epub.Start();

				await bob.AddStylesheetFromFile("stylesheet.css", "stylesheet.css");
				await bob.AddCoverImage("cover.jpg", CoverImage(vol));

				await bob.AddChapter("Color Illustrations", async c =>
				{
					var inserts = Directory.GetFiles(Forwards(vol));
					foreach (var file in inserts)
						await c.AddImage(Path.GetFileName(file), file);

					var contents = ContentsImage(vol);
					if (File.Exists(contents))
						await c.AddImage("Contents.jpg", contents);
				});

				for (var i = 0; i < chaps.Length; i++)
				{
					var chap = chaps[i];
					await bob.AddChapter(chap.Chapter, async (c) =>
					{
						await c.AddRawPage($"chapter{i}.xhtml", $"<h1>{chap.Chapter}</h1>{CleanContents(chap.Content, chap.Chapter)}");
					});
				}

				await bob.AddChapter("Inserts", async c =>
				{
					var inserts = Directory.GetFiles(Inserts(vol));
					foreach (var file in inserts)
						await c.AddImage(Path.GetFileName(file), file);
				});
			}
		
			_logger.LogInformation($"Finished making vol {vol}");
		}

		public string CleanContents(string content, string chap)
		{
			content = content
				.Replace("<hr>", "")
				.Replace("<hr/>", "")
				.Replace("<hr />", "");

			int i = 0;
			while(i < content.Length)
			{
				var fis = content.IndexOf("<p>", i);
				var nis = content.IndexOf("<p>", fis + 3);
				var fie = content.IndexOf("</p>", fis);

				if (fis == -1 || nis == -1) break;

				if (nis < fie)
				{
					content = content.Insert(nis, "</p>");
					_logger.LogWarning($"Found culpret: {chap} FIS: {fis} - NIS: {nis} - FIE: {fie}");
				}

				if (fie == -1)
				{
					_logger.LogWarning("What?");
					break;
				}

				i = fie + 1;
			}

			return content;
		}
	}
}
