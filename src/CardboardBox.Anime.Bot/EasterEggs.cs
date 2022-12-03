using F23.StringSimilarity;

namespace CardboardBox.Anime.Bot
{
	using Manga;
	using Manga.Providers;
	using Services;

	public class EasterEggs
	{
		private readonly DiscordSocketClient _client;
		private readonly IGoogleVisionService _vision;
		private readonly IDiscordApiService _api;
		private readonly IMangaDexSource _mangadex;
		private readonly IMangaUtilityService _util;

		public EasterEggs(
			DiscordSocketClient client, 
			IGoogleVisionService vision,
			IDiscordApiService api,
			IMangaDexSource mangadex,
			IMangaUtilityService util)
		{
			_client = client;
			_vision = vision;
			_api = api;
			_mangadex = mangadex;
			_util = util;
		}

		public Task Setup()
		{
			_client.MessageReceived += _client_MessageReceived;
			return Task.CompletedTask;
		}

		private Task _client_MessageReceived(SocketMessage arg)
		{
			return Task.Run(() => HandOff(arg));
		}

		public async void HandOff(SocketMessage arg)
		{
			var reference = new MessageReference(arg.Id, arg.Channel.Id);
			if (arg.Author.IsBot) return;

			if (arg.Content.ToLower().Contains("https://ifunny.co/video/"))
				await HandleVideo(arg, reference);

			if (arg.Content.ToLower().Contains("https://ifunny.co/picture/"))
				await HandleImage(arg, reference);

			if (!arg.MentionedUsers.Any(t => t.Id == _client.CurrentUser.Id))
				return;

			if (arg.Reference == null || !arg.Reference.MessageId.IsSpecified)
				return;

			var msg = await arg.Channel.GetMessageAsync(arg.Reference.MessageId.Value);

			if (arg.Content.ToLower().Contains("lookup"))
			{
				await HandleMangaLookup(msg, arg, reference);
				return;
			}

			await HandleEmotes(msg, arg, reference);
		}

		public string DetermineExtension(StickerFormatType type)
		{
			return type switch
			{
				StickerFormatType.Png => "png",
				_ => "webp",
			};
		}

		public async Task HandleMangaLookup(IMessage msg, SocketMessage rpl, MessageReference refe)
		{
			if (rpl.Channel is not SocketGuildChannel guild)
			{
				await msg.Channel.SendMessageAsync("This can only be used within servers.", messageReference: refe);
				return;
			}

			var settings = await _api.Settings(guild.Guild.Id);
			if (settings == null || !settings.EnableLookup)
			{
				await msg.Channel.SendMessageAsync("This feature is not enabled within this server. Contact Cardboard to get it enabled!", messageReference: refe);
				return;
			}

			if (msg.Attachments.Count == 0)
			{
				await msg.Channel.SendMessageAsync("The tagged message has no attachments!", messageReference: refe);
				return;
			}

			var img = msg.Attachments.FirstOrDefault(t => t.ContentType.StartsWith("image"));
			if (img == null)
			{
				await msg.Channel.SendMessageAsync("I don't see any image attachments on the tagged message.", messageReference: refe);
				return;
			}

			var mod = await msg.Channel.SendMessageAsync("<a:loading:1048471999065903244> Processing your request...", messageReference: refe);

			var request = await _vision.ExecuteVisionRequest(img.Url);
			if (request == null)
			{
				await mod.ModifyAsync(t => t.Content = "I couldn't find any matches for that image.");
				return;
			}

			if (await CheckForMangaDex(request, mod))
				return;

			var bob = new EmbedBuilder()
				.WithTitle("Manga Lookup Request")
				.WithDescription($"My best guess is: {request.Guess} ({request.Score * 100:0.00}%)")
				.WithImageUrl(img.Url)
				.WithCurrentTimestamp()
				.WithAuthor(msg.Author)
				.WithUrl("https://cba.index-0.com/manga");

			for (var i = 0; i < request.WebPages.Length && i < 5; i++)
			{
				var cur = request.WebPages[i];
				bob.AddField("Result #" + (i + 1), $"[{cur.Title}]({cur.Url})");
			}

			await mod.ModifyAsync(t =>
			{
				t.Content = "Here are your lookup results: ";
				t.Embed = bob.Build();
			});
		}

		public async Task<bool> CheckForMangaDex(VisionResults results, IUserMessage mod)
		{
			for(var i = 0; i < results.WebPages.Length && i < 5; i++)
			{
				var (url, current) = results.WebPages[i];
				var modTitle = PurgeCharacters(current);
				if (string.IsNullOrEmpty(modTitle)) continue;

				var search = await _mangadex.Search(modTitle);
				if (search == null || search.Length == 0) continue;

				var sort = Rank(modTitle, search)
					.OrderByDescending(t => t.Compute)
					.ToArray();
				var (compute, main, manga) = sort.First();

				await mod.ModifyAsync(t =>
				{
					t.Content = $"Here you go:\r\nThe title I found was: \"{current}\"\r\nI found it here: {url}\r\nCompute Cof: {compute:0.0}";
					t.Embed = _util.GenerateEmbed(manga, false).Build();
				});

				return true;
			}

			return false;
		}

		public IEnumerable<(double Compute, bool Main, Manga Manga)> Rank(string title, Manga[] manga)
		{
			var check = new NormalizedLevenshtein();

			foreach (var m in manga)
			{
				var mt = m.Title.ToLower();
				if (mt == title)
				{
					yield return (1.2, true, m);
					continue;
				}

				yield return (check.Distance(title, mt), true, m);

				foreach (var t in m.AltTitles)
				{
					var mtt = t.ToLower();
					if (mtt == title)
					{
						yield return (1.1, false, m);
						continue;
					}

					yield return (check.Distance(title, mtt), false, m);
				}
			}
		}

		public string PurgeCharacters(string title)
		{
			title = title.ToLower();
			if (title.Contains("chapter")) title = title.Split(new[] { "chapter", "-", ":" }, StringSplitOptions.RemoveEmptyEntries).First();

			return Regex.Replace(title, "[^a-zA-Z0-9 ]", string.Empty);
		}

		public async Task HandleEmotes(IMessage msg, SocketMessage rpl, MessageReference refe)
		{
			var bob = new StringBuilder();

			foreach (var sticker in msg.Stickers)
				bob.AppendLine($"https://media.discordapp.net/stickers/{sticker.Id}.{DetermineExtension(sticker.Format)}");

			var emoteRegex = new Regex("<(a)?:(.*?):([0-9]{1,})>");
			var matches = emoteRegex.Matches(msg.Content);

			foreach(Match match in matches)
			{
				var isAnimated = match.Groups[1].Value == "a";
				var name = match.Groups[2].Value;
				var id = match.Groups[3].Value;
				var ext = isAnimated ? "gif" : "png";
				var url = $"https://cdn.discordapp.com/emojis/{id}.{ext}";
				bob.AppendLine($"Emote: {name} - {url}");
			}

			var compiled = bob.ToString();
			if (string.IsNullOrEmpty(compiled)) return;

			if (rpl.Channel is not SocketGuildChannel guild)
			{
				await msg.Channel.SendMessageAsync("This can only be used within servers.", messageReference: refe);
				return;
			}

			var settings = await _api.Settings(guild.Guild.Id);
			if (settings == null || !settings.EnableTheft)
			{
				await msg.Channel.SendMessageAsync("This feature is not enabled within this server. Contact Cardboard to get it enabled!", messageReference: refe);
				return;
			}

			await rpl.Channel.SendMessageAsync(compiled, messageReference: refe);
		}

		public async Task HandleVideo(SocketMessage msg, MessageReference refe)
		{
			var regex = new Regex("https://ifunny.co/video/(.*?)?s=cl");
			var match = regex.Match(msg.Content);
			if (!match.Success) return;

			var url = match.Value;
			var html = new HtmlWeb();
			var doc = (await html.LoadFromWebAsync(url)).DocumentNode;

			var video = doc.SelectSingleNode("//video[@class='Fv7s']")?.Attributes["data-src"]?.Value;
			if (!string.IsNullOrEmpty(video))
				await msg.Channel.SendMessageAsync(video, messageReference: refe);
		}

		public async Task HandleImage(SocketMessage msg, MessageReference refe)
		{
			var regex = new Regex("https://ifunny.co/picture/(.*?)?s=cl");
			var match = regex.Match(msg.Content);
			if (!match.Success) return;

			var url = match.Value;
			var html = new HtmlWeb();
			var doc = (await html.LoadFromWebAsync(url)).DocumentNode;

			var img = doc.SelectSingleNode("//img[@class='f+2d']")?.Attributes["src"]?.Value;
			if (!string.IsNullOrEmpty(img))
				await msg.Channel.SendMessageAsync(img, messageReference: refe);
		}
	}
}
