using F23.StringSimilarity;
using System.Net;

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
		private readonly ILogger _logger;

		public EasterEggs(
			DiscordSocketClient client, 
			IGoogleVisionService vision,
			IDiscordApiService api,
			IMangaDexSource mangadex,
			IMangaUtilityService util,
			ILogger<EasterEggs> logger)
		{
			_client = client;
			_vision = vision;
			_api = api;
			_mangadex = mangadex;
			_util = util;
			_logger = logger;
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

		public string? DetermineUrl(IMessage msg)
		{
			var img = msg.Attachments.FirstOrDefault(t => t.ContentType.StartsWith("image"));
			if (img != null) return img.Url;

			var content = msg.Content;
			if (Uri.IsWellFormedUriString(content, UriKind.Absolute)) return msg.Content;

			return null;
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

			var img = DetermineUrl(msg);
			if (img == null)
			{
				await msg.Channel.SendMessageAsync("I don't see any image attachments or URLs on the tagged message.", messageReference: refe);
				return;

			}

			var mod = await msg.Channel.SendMessageAsync("<a:loading:1048471999065903244> Processing your request...", messageReference: refe);

			try
			{
				var request = await _vision.ExecuteVisionRequest(img);
				if (request == null)
				{
					await mod.ModifyAsync(t => t.Content = "I couldn't find any matches for that image.");
					return;
				}

				if (await CheckForMangaDex(request, mod, img, msg))
					return;

				var bob = new EmbedBuilder()
					.WithTitle("Manga Lookup Request")
					.WithDescription($"My best guess is: {request.Guess} ({request.Score * 100:0.00}%)")
					.WithImageUrl(img)
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
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error occurred during manga lookup: {img}");
				await mod.ModifyAsync(t =>
				{
					t.Content = "Something went wrong! Contact Cardboard for more assistance or try again later!\r\n" +
					"Error Message: " + ex.Message;
				});
			}
		}

		public async Task<bool> CheckForMangaDex(VisionResults results, IUserMessage mod, string imageUrl, IMessage target)
		{
			var found = new Dictionary<string, (double, bool, Manga)>();
			var used = new List<(string url, string title)>();
			for(var i = 0; i < results.WebPages.Length && i < 5; i++)
			{
				var (url, current) = results.WebPages[i];
				var modTitle = PurgeCharacters(current);
				if (string.IsNullOrEmpty(modTitle)) continue;

				var search = Array.Empty<Manga>();

				try
				{
					search = await _mangadex.Search(modTitle);
					if (search == null || search.Length == 0) continue;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error while processing manga dex results: {results.Guess} >> {current} >> {url}");
					continue;
				}

				var sort = Rank(modTitle, search)
					.OrderByDescending(t => t.Compute)
					.ToArray();
				var (compute, main, manga) = sort.First();
				for(var x = 0; x < sort.Length && x < 3; x++)
				{
					var cur = sort[x];
					if (cur.Compute == 1.2)
					{
						var searchEmbed = new EmbedBuilder()
							.WithTitle("Manga Search Results")
							.WithDescription($"Here is what I found [{current}]({url}). (CF: {compute:0.00}. MT: {main})")
							.WithAuthor(target.Author)
							.WithCurrentTimestamp()
							.WithFooter("Cardboard Box | Manga")
							.WithThumbnailUrl(imageUrl);

						await mod.ModifyAsync(t =>
						{
							t.Content = $"Here you go:";
							t.Embeds = new Embed[]
							{
						searchEmbed.Build(),
						_util.GenerateEmbed(manga, false).Build()
							};
						});
						return true;
					}

					if (found.ContainsKey(cur.Manga.Id)) continue;

					found.Add(cur.Manga.Id, cur);
					used.Add((url, current));
				}

			}

			if (found.Count == 0) return false;

			var distinctUsed = used.DistinctBy(t => t.url).ToArray();
			if (distinctUsed.Length == 1 && found.Count == 1)
			{
				var (url, title) = distinctUsed.First();
				var (compute, main, manga) = found.First().Value;
				var searchEmbed = new EmbedBuilder()
					.WithTitle("Manga Search Results")
					.WithDescription($"Here is what I found [{title}]({url}). (CF: {compute:0.00}. MT: {main})")
					.WithAuthor(target.Author)
					.WithCurrentTimestamp()
					.WithFooter("Cardboard Box | Manga")
					.WithThumbnailUrl(imageUrl);

				await mod.ModifyAsync(t =>
				{
					t.Content = $"Here you go:";
					t.Embeds = new Embed[]
					{
						searchEmbed.Build(),
						_util.GenerateEmbed(manga, false).Build()
					};
				});
				return true;
			}

			var titleList = string.Join("\r\n", used
				.DistinctBy(t => t.url)
				.Select(t => $"[{t.title}]({t.url})"));

			var e = new EmbedBuilder()
				.WithTitle("Manga Search Results")
				.WithThumbnailUrl(imageUrl)
				.WithDescription($"Here is what I found:\r\n{titleList}\r\nBelow are some results from MangaDex: ")
				.WithAuthor(target.Author)
				.WithFooter("Cardboard Box | Manga")
				.WithCurrentTimestamp();

			foreach(var item in found)
			{
				var (comp, main, manga) = item.Value;
				e.AddOptField(manga.Title, $"[Link](https://mangadex.org/title/{item.Key}) (CF: {comp:0.00}. MT: {main})");
			}

			await mod.ModifyAsync(t =>
			{
				t.Content = $"Here you go:";
				t.Embed = e.Build();
			});

			return true;
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
			var regexPurgers = new[]
			{
				("manga", "manga[a-z]{1,}\\b")
			};

			var purgers = new[]
			{
				("chapter", new[] { "chapter" }),
				("read", new[] { "read" }),
				("online", new[] { "online" }),
				("manga", new[] { "manga" })
			};

			title = title.ToLower();

			if (title.Contains("<"))
			{
				var doc = new HtmlDocument();
				doc.LoadHtml(title);
				title = doc.DocumentNode.InnerText;
			}


			if (title.Contains("&")) title = WebUtility.HtmlDecode(title);

			foreach (var (text, regex) in regexPurgers)
				if (title.Contains(text))
					title = Regex.Replace(title, regex, string.Empty);

			foreach (var (text, replacers) in purgers)
				if (title.Contains(text))
					foreach(var regex in replacers)
						title = title.Replace(regex, "").Trim();

			return new string(title
				.Where(t => 
					!char.IsPunctuation(t) && 
					!char.IsNumber(t) &&
					!char.IsSymbol(t))
				.ToArray())
				.Trim();
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
