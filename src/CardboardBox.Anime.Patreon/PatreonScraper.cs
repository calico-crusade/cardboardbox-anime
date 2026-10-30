using System.Globalization;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace CardboardBox.Anime.Patreon;

public sealed partial class PatreonScraper : IPatreonScraper
{
	private static readonly string[] ContentSelectors =
	[
		"[data-tag='post-content']",
		"[data-testid='post-content']",
		"[data-testid*='post-content']",
		"div[class*='additionalStylesPostContentWrapper']",
		"main article div[class*='RichText']",
	];

	public async Task<PatreonScrapeResult> ScrapeAsync(PatreonScrapeOptions options, CancellationToken token = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(options.RootDirectory);
		if (!Directory.Exists(options.RootDirectory))
			throw new DirectoryNotFoundException($"The Patreon load directory does not exist: {options.RootDirectory}");

		var profileDirectory = options.BrowserProfileDirectory;
		if (string.IsNullOrWhiteSpace(profileDirectory))
		{
			var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			profileDirectory = Path.Combine(localData, "CardboardBox.Anime", "PatreonBrowser");
		}

		Directory.CreateDirectory(profileDirectory);
		var browserChannel = options.BrowserChannel;
		string? browserExecutablePath = null;
		if (options.WaitForLogin)
		{
			var loginBrowser = ResolveLoginBrowser(options);
			if (string.IsNullOrWhiteSpace(browserChannel))
			{
				if (string.IsNullOrWhiteSpace(options.LoginBrowserExecutablePath))
					browserChannel = loginBrowser.Channel;
				else
					browserExecutablePath = loginBrowser.Path;
			}
			await RunInteractiveLoginAsync(loginBrowser.Path, profileDirectory, options, token);
		}
		else if (string.IsNullOrWhiteSpace(browserChannel))
		{
			browserChannel = ResolveInstalledBrowser()?.Channel;
		}

		using var playwright = await Playwright.CreateAsync();
		await using var context = await LaunchContextAsync(
			playwright,
			profileDirectory,
			browserChannel,
			browserExecutablePath,
			options);
		var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
		page.SetDefaultTimeout(30_000);
		page.SetDefaultNavigationTimeout(60_000);

		var results = new List<PatreonSeriesScrapeResult>();
		var metadataFiles = Directory.GetFiles(options.RootDirectory, "meta.txt", SearchOption.TopDirectoryOnly)
			.Concat(Directory.GetDirectories(options.RootDirectory, "*", SearchOption.TopDirectoryOnly)
				.Select(directory => Path.Combine(directory, "meta.txt"))
				.Where(File.Exists))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		if (metadataFiles.Length == 0)
			throw new InvalidDataException($"No meta.txt files were found beneath {options.RootDirectory}.");

		foreach (var metadataFile in metadataFiles)
		{
			token.ThrowIfCancellationRequested();
			results.Add(await ScrapeSeriesSafelyAsync(page, metadataFile, options, token));
		}

		return new PatreonScrapeResult(results);
	}

	private static async Task<IBrowserContext> LaunchContextAsync(
		IPlaywright playwright,
		string profileDirectory,
		string? browserChannel,
		string? browserExecutablePath,
		PatreonScrapeOptions options)
	{
		try
		{
			return await playwright.Chromium.LaunchPersistentContextAsync(profileDirectory, new()
			{
				Headless = options.Headless,
				Channel = string.IsNullOrWhiteSpace(browserChannel) ? null : browserChannel,
				ExecutablePath = browserExecutablePath,
				ViewportSize = ViewportSize.NoViewport,
			});
		}
		catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException(
				"The Playwright Chromium browser is not installed. Build the CLI, then run its generated " +
				"playwright.ps1 script with 'install chromium', or pass --browser-channel msedge/chrome.", ex);
		}
	}

	private static async Task<PatreonSeriesScrapeResult> ScrapeSeriesSafelyAsync(
		IPage page,
		string metadataFile,
		PatreonScrapeOptions options,
		CancellationToken token)
	{
		PatreonSeriesMetadata? metadata = null;
		try
		{
			metadata = await PatreonSeriesMetadata.LoadAsync(metadataFile, token);
			if (string.IsNullOrWhiteSpace(metadata.SourceUrl))
				throw new InvalidDataException("meta.txt must contain either a Collection or Posts URL.");

			return await ScrapeSeriesAsync(page, metadata, options, token);
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			throw;
		}
		catch (PatreonAuthenticationException)
		{
			throw;
		}
		catch (PlaywrightException ex) when (
			ex.GetType().Name.Equals("TargetClosedException", StringComparison.Ordinal) ||
			ex.Message.Contains("has been closed", StringComparison.OrdinalIgnoreCase))
		{
			throw;
		}
		catch (Exception ex)
		{
			return new PatreonSeriesScrapeResult(
				metadata?.Directory ?? Path.GetDirectoryName(metadataFile)!,
				metadata?.SourceUrl,
				[],
				0,
				ex.Message);
		}
	}

	private static async Task<PatreonSeriesScrapeResult> ScrapeSeriesAsync(
		IPage page,
		PatreonSeriesMetadata metadata,
		PatreonScrapeOptions options,
		CancellationToken token)
	{
		var startedAt = DateTimeOffset.UtcNow;
		Report(options, $"Opening {metadata.SourceUrl}", metadata);
		await NavigateAsync(page, metadata.SourceUrl!);
		await WaitForSecurityVerificationAsync(page, options, metadata, metadata.SourceUrl!, token);
		await EnsureAuthenticatedAsync(page, options);

		var postUrls = await CollectPostUrlsAsync(page, options, token);
		Report(options, $"Found {postUrls.Count} candidate posts", metadata);
		if (postUrls.Count == 0)
		{
			throw new InvalidDataException(
				"No Patreon post links were found. The source may be unavailable, the login may lack access, or Patreon may have changed its page markup.");
		}

		var state = await PatreonScrapeState.LoadAsync(metadata.Directory, token);
		var initialPostIdWatermark = state.PostIdWatermark;
		var written = new List<string>();
		var skipped = 0;
		var extracted = 0;
		var attempted = 0;
		var existingBoundary = GetLatestExistingChapterBoundary(metadata.Directory);
		var latestExistingChapter = existingBoundary?.ChapterNumber;
		var initialLatestExistingChapter = latestExistingChapter;
		foreach (var discoveredPost in postUrls)
		{
			token.ThrowIfCancellationRequested();
			var postId = GetPostId(discoveredPost.Url);
			if (!state.Contains(postId) &&
				initialPostIdWatermark is { } postIdWatermark &&
				long.TryParse(postId, out var numericPostId) &&
				numericPostId <= postIdWatermark)
			{
				skipped++;
				continue;
			}

			if (existingBoundary is not null &&
				discoveredPost.PublishedAt is { } listingPublishedAt &&
				listingPublishedAt <= existingBoundary.PublishedAt)
			{
				await RecordSeenAsync(state, postId, token);
				skipped++;
				continue;
			}

			var discoveredChapterNumbers = GetDiscoveredChapterNumbers(discoveredPost);
			var likelyChapterNumber = GetLikelyChapterNumber(discoveredChapterNumbers, latestExistingChapter);
			var chapterFileExists = likelyChapterNumber is not null &&
				File.Exists(DetermineOutputPath(metadata.Directory, likelyChapterNumber));
			if (initialLatestExistingChapter is { } listingChapterBoundary &&
				decimal.TryParse(likelyChapterNumber, NumberStyles.Number, CultureInfo.InvariantCulture, out var listingChapterNumber) &&
				listingChapterNumber <= listingChapterBoundary)
			{
				await RecordSeenAsync(state, postId, token);
				skipped++;
				Report(
					options,
					$"Reached existing chapter boundary {listingChapterBoundary} on the listing page; stopping this series",
					metadata,
					discoveredPost.Url);
				break;
			}

			var recheckRecordedPost = false;
			if (state.Contains(postId))
			{
				var missingNewChapterNumber = GetLikelyNewerChapterNumber(
					discoveredChapterNumbers,
					initialLatestExistingChapter);
				if (missingNewChapterNumber is null)
				{
					skipped++;
					continue;
				}

				Report(
					options,
					$"Rechecking recorded post because {missingNewChapterNumber}.html is missing",
					metadata,
					discoveredPost.Url);
				recheckRecordedPost = true;
			}
			if (!recheckRecordedPost && chapterFileExists)
			{
				await RecordSeenAsync(state, postId, token);
				skipped++;
				continue;
			}

			attempted++;
			Report(options, $"Reading new post: {DescribeDiscoveredPost(discoveredPost, discoveredChapterNumbers)}", metadata, discoveredPost.Url);
			await NavigatePostAsync(page, discoveredPost.Url);
			await WaitForSecurityVerificationAsync(page, options, metadata, discoveredPost.Url, token);
			await WaitForPostDocumentAsync(page, discoveredPost.Url);

			var post = await ExtractPostAsync(page, discoveredPost.Url);
			if (post is null)
			{
				skipped++;
				continue;
			}
			extracted++;
			var chapterNumber = GetChapterNumber(post.Title);
			if (string.IsNullOrWhiteSpace(chapterNumber))
			{
				if (!post.Title.Any(char.IsDigit))
					await RecordSeenAsync(state, post.Id, token);
				skipped++;
				Report(options, $"Skipping post without a chapter number: {post.Title}", metadata, discoveredPost.Url);
				continue;
			}

			if (initialLatestExistingChapter is { } chapterBoundary &&
				decimal.TryParse(chapterNumber, NumberStyles.Number, CultureInfo.InvariantCulture, out var extractedChapterNumber) &&
				extractedChapterNumber <= chapterBoundary)
			{
				await RecordSeenAsync(state, post.Id, token);
				skipped++;
				Report(
					options,
					$"Reached existing chapter boundary {chapterBoundary}; skipping historical chapter {chapterNumber} and stopping this series",
					metadata,
					discoveredPost.Url);
				break;
			}

			var path = DetermineOutputPath(metadata.Directory, chapterNumber);
			if (File.Exists(path))
			{
				await RecordSeenAsync(state, post.Id, token);
				skipped++;
				continue;
			}

			var html = $"<h1>{WebUtility.HtmlEncode(post.Title)}</h1>{Environment.NewLine}{post.ContentHtml}";
			await File.WriteAllTextAsync(path, html, new UTF8Encoding(false), token);
			if (post.PublishedAt is { } fileDate)
				File.SetLastWriteTimeUtc(path, fileDate.UtcDateTime);

			await RecordSeenAsync(state, post.Id, token);
			written.Add(path);
			if (decimal.TryParse(chapterNumber, NumberStyles.Number, CultureInfo.InvariantCulture, out var writtenChapterNumber) &&
				(latestExistingChapter is null || writtenChapterNumber > latestExistingChapter))
			{
				latestExistingChapter = writtenChapterNumber;
			}
			Report(options, $"Wrote {Path.GetFileName(path)}", metadata, discoveredPost.Url);
		}

		if (attempted > 0 && extracted == 0)
		{
			throw new InvalidDataException(
				"Post links were found, but no readable post title/content pairs could be extracted. Last Load was not changed.");
		}

		await state.SaveAsync(token);
		metadata.SetLastLoad(startedAt);
		await metadata.SaveAsync(token);
		return new PatreonSeriesScrapeResult(metadata.Directory, metadata.SourceUrl, written, skipped);
	}

	private static async Task NavigateAsync(IPage page, string url)
	{
		await page.GotoAsync(url, new()
		{
			WaitUntil = WaitUntilState.DOMContentLoaded,
			Timeout = 30_000,
		});
	}

	private static async Task NavigatePostAsync(IPage page, string url)
	{
		await page.GotoAsync(url, new()
		{
			WaitUntil = WaitUntilState.Commit,
			Timeout = 15_000,
		});
	}

	private static async Task WaitForPostDocumentAsync(IPage page, string postUrl)
	{
		try
		{
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 15_000 });
		}
		catch (TimeoutException ex)
		{
			throw new InvalidDataException(
				$"Patreon did not finish loading the post document within 15 seconds: {postUrl}",
				ex);
		}
	}

	private static async Task EnsureAuthenticatedAsync(IPage page, PatreonScrapeOptions options)
	{
		if (!await RequiresLoginAsync(page))
			return;

		var message = options.WaitForLogin
			? "Patreon was still signed out after the regular login browser closed. Re-run with --wait-for-login and finish signing in before closing it."
			: "The dedicated Patreon browser profile is not logged in. Re-run with --wait-for-login to log in using a regular browser.";
		throw new PatreonAuthenticationException(message);
	}

	private static async Task WaitForSecurityVerificationAsync(
		IPage page,
		PatreonScrapeOptions options,
		PatreonSeriesMetadata metadata,
		string postUrl,
		CancellationToken token)
	{
		await Task.Delay(750, token);
		if (!await IsSecurityVerificationAsync(page))
			return;

		if (options.Headless)
		{
			throw new InvalidDataException(
				$"Patreon presented a Cloudflare security verification page for {postUrl}. " +
				"Re-run without --headless and complete the verification in the browser window.");
		}

		Report(
			options,
			"Cloudflare security verification is waiting in the browser; complete it to continue",
			metadata,
			postUrl);
		var deadline = DateTimeOffset.UtcNow + options.LoginTimeout;
		while (await IsSecurityVerificationAsync(page))
		{
			if (DateTimeOffset.UtcNow >= deadline)
			{
				throw new InvalidDataException(
					$"Cloudflare security verification was not completed within {options.LoginTimeout.TotalMinutes:N0} minutes.");
			}

			await Task.Delay(1_000, token);
		}

		await Task.Delay(750, token);
	}

	private static async Task<bool> IsSecurityVerificationAsync(IPage page)
	{
		var title = await page.TitleAsync();
		if (title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase))
			return true;

		return await page.GetByText("Performing security verification", new() { Exact = false }).CountAsync() > 0 ||
			await page.GetByText("Verifying you are human", new() { Exact = false }).CountAsync() > 0;
	}

	private static async Task RunInteractiveLoginAsync(
		string browserPath,
		string profileDirectory,
		PatreonScrapeOptions options,
		CancellationToken token)
	{
		options.Progress?.Invoke(new PatreonScrapeProgress(
			"Opening a regular browser for Patreon login; finish signing in, then close that browser to continue"));

		var startInfo = new ProcessStartInfo
		{
			FileName = browserPath,
			UseShellExecute = false,
		};
		startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
		startInfo.ArgumentList.Add("--no-first-run");
		startInfo.ArgumentList.Add("--no-default-browser-check");
		startInfo.ArgumentList.Add("https://www.patreon.com/login");

		using var process = Process.Start(startInfo) ??
			throw new PatreonAuthenticationException($"Unable to start the login browser: {browserPath}");
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
		timeout.CancelAfter(options.LoginTimeout);
		try
		{
			await process.WaitForExitAsync(timeout.Token);
		}
		catch (OperationCanceledException) when (!token.IsCancellationRequested)
		{
			try
			{
				process.Kill(entireProcessTree: true);
			}
			catch (InvalidOperationException)
			{
				// The browser exited between the timeout and cleanup attempt.
			}

			throw new PatreonAuthenticationException(
				$"The regular login browser was not closed within {options.LoginTimeout.TotalMinutes:N0} minutes.");
		}
	}

	private static LoginBrowser ResolveLoginBrowser(PatreonScrapeOptions options)
	{
		if (!string.IsNullOrWhiteSpace(options.LoginBrowserExecutablePath))
		{
			var path = Path.GetFullPath(options.LoginBrowserExecutablePath);
			if (!File.Exists(path))
				throw new PatreonAuthenticationException($"The login browser does not exist: {path}");

			return new LoginBrowser(path, InferBrowserChannel(path));
		}

		var browser = ResolveInstalledBrowser(options.BrowserChannel);
		return browser ?? throw new PatreonAuthenticationException(
			"A regular Google Chrome or Microsoft Edge installation was not found. Pass its executable path with --login-browser.");
	}

	private static LoginBrowser? ResolveInstalledBrowser(string? requestedChannel = null)
	{
		var normalized = requestedChannel?.ToLowerInvariant();
		var includeChrome = string.IsNullOrWhiteSpace(normalized) || normalized.StartsWith("chrome");
		var includeEdge = string.IsNullOrWhiteSpace(normalized) || normalized.StartsWith("msedge");
		var candidates = new List<LoginBrowser>();

		if (OperatingSystem.IsWindows())
		{
			if (includeChrome)
			{
				AddBrowser(candidates, Environment.SpecialFolder.LocalApplicationData, @"Google\Chrome\Application\chrome.exe", "chrome");
				AddBrowser(candidates, Environment.SpecialFolder.ProgramFiles, @"Google\Chrome\Application\chrome.exe", "chrome");
				AddBrowser(candidates, Environment.SpecialFolder.ProgramFilesX86, @"Google\Chrome\Application\chrome.exe", "chrome");
			}

			if (includeEdge)
			{
				AddBrowser(candidates, Environment.SpecialFolder.ProgramFilesX86, @"Microsoft\Edge\Application\msedge.exe", "msedge");
				AddBrowser(candidates, Environment.SpecialFolder.ProgramFiles, @"Microsoft\Edge\Application\msedge.exe", "msedge");
			}
		}
		else if (OperatingSystem.IsMacOS())
		{
			if (includeChrome)
				candidates.Add(new LoginBrowser("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "chrome"));
			if (includeEdge)
				candidates.Add(new LoginBrowser("/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge", "msedge"));
		}
		else
		{
			if (includeChrome)
			{
				candidates.Add(new LoginBrowser("/usr/bin/google-chrome", "chrome"));
				candidates.Add(new LoginBrowser("/usr/bin/google-chrome-stable", "chrome"));
			}
			if (includeEdge)
				candidates.Add(new LoginBrowser("/usr/bin/microsoft-edge", "msedge"));
		}

		return candidates.FirstOrDefault(candidate => File.Exists(candidate.Path));
	}

	private static void AddBrowser(
		ICollection<LoginBrowser> candidates,
		Environment.SpecialFolder root,
		string relativePath,
		string channel)
	{
		var basePath = Environment.GetFolderPath(root);
		if (!string.IsNullOrWhiteSpace(basePath))
			candidates.Add(new LoginBrowser(Path.Combine(basePath, relativePath), channel));
	}

	private static string InferBrowserChannel(string path) =>
		Path.GetFileName(path).Contains("edge", StringComparison.OrdinalIgnoreCase) ? "msedge" : "chrome";

	private static async Task<bool> RequiresLoginAsync(IPage page)
	{
		if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
			return true;

		if (await page.Locator("input[type='password']").CountAsync() > 0)
			return true;

		return await page.Locator(
			"a[href*='/login'], a[href*='auth.patreon.com'], header a:has-text('Log in'), nav a:has-text('Log in')")
			.CountAsync() > 0;
	}

	private static async Task<IReadOnlyList<DiscoveredPost>> CollectPostUrlsAsync(
		IPage page,
		PatreonScrapeOptions options,
		CancellationToken token)
	{
		var posts = new List<DiscoveredPost>();
		var known = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var unchangedPasses = 0;
		try
		{
			await page.Locator("a[href*='/posts/']").First.WaitForAsync(new()
			{
				State = WaitForSelectorState.Attached,
				Timeout = 10_000,
			});
		}
		catch (TimeoutException)
		{
			return posts;
		}

		for (var scroll = 0; scroll <= options.MaxScrolls && posts.Count < options.MaxPostsPerSeries; scroll++)
		{
			token.ThrowIfCancellationRequested();
			await page.WaitForTimeoutAsync(750);
			var links = await page.Locator("a[href*='/posts/']").EvaluateAllAsync<ListingPost[]>(
				"""
				elements => elements.map(element => {
					const card = element.closest("article, [data-tag*='post-card'], [data-testid*='post-card']");
					const heading = card?.querySelector("[data-tag*='post-title'], [data-testid*='post-title'], h1, h2, h3");
					const time = card?.querySelector('time');
					return {
						Href: element.href,
						Title: (heading?.textContent || element.getAttribute('aria-label') || element.textContent || '').trim(),
						PublishedAt: time?.getAttribute('datetime') || time?.textContent || null
					};
				})
				""");

			var countBefore = posts.Count;
			foreach (var link in links)
			{
				var normalized = NormalizePostUrl(link.Href);
				if (normalized is null)
					continue;

				if (!known.TryGetValue(normalized, out var index))
				{
					known[normalized] = posts.Count;
					posts.Add(new DiscoveredPost(normalized, link.Title, ParseListingDate(link.PublishedAt)));
				}
				else
				{
					var current = posts[index];
					posts[index] = current with
					{
						Title = TitleScore(link.Title) > TitleScore(current.Title) ? link.Title : current.Title,
						PublishedAt = current.PublishedAt ?? ParseListingDate(link.PublishedAt),
					};
				}
			}

			unchangedPasses = posts.Count == countBefore ? unchangedPasses + 1 : 0;
			if (unchangedPasses >= 3 || scroll == options.MaxScrolls || posts.Count >= options.MaxPostsPerSeries)
				break;

			await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
		}

		return posts
			.OrderByDescending(post => GetNumericPostId(post.Url))
			.ThenByDescending(post => post.PublishedAt)
			.Take(options.MaxPostsPerSeries)
			.ToArray();
	}

	private static long GetNumericPostId(string postUrl) =>
		long.TryParse(GetPostId(postUrl), out var postId) ? postId : long.MinValue;

	private static IReadOnlyList<string> GetDiscoveredChapterNumbers(DiscoveredPost post)
	{
		var numbers = new List<string>();
		void Add(string? value)
		{
			if (!string.IsNullOrWhiteSpace(value) &&
				!numbers.Contains(value, StringComparer.OrdinalIgnoreCase))
			{
				numbers.Add(value);
			}
		}

		var slugMatch = PostSlugRegex().Match(new Uri(post.Url).AbsolutePath);
		if (slugMatch.Success)
		{
			var slug = Uri.UnescapeDataString(slugMatch.Groups[1].Value);
			Add(GetChapterNumber(slug.Replace('-', ' ')));
		}

		if (string.IsNullOrWhiteSpace(post.Title))
			return numbers;

		foreach (Match match in ChapterNumberRegex().Matches(post.Title))
			Add(match.Groups[1].Value);
		foreach (Match match in NamedChapterNumberRegex().Matches(post.Title))
			Add(match.Groups[1].Value);
		foreach (Match match in BracketedChapterNumberRegex().Matches(post.Title))
			Add(match.Groups[1].Value);
		foreach (Match match in NumberedPostTitleRegex().Matches(post.Title))
			Add(match.Groups[1].Value);
		if (post.Title.Length <= 300)
			Add(GetChapterNumber(post.Title));

		return numbers;
	}

	private static ExistingChapterBoundary? GetLatestExistingChapterBoundary(string directory) =>
		Directory.GetFiles(directory, "*.html", SearchOption.TopDirectoryOnly)
			.Select(path => new
			{
				Path = path,
				ChapterNumber = decimal.TryParse(
					Path.GetFileNameWithoutExtension(path),
					NumberStyles.Number,
					CultureInfo.InvariantCulture,
					out var chapterNumber)
						? chapterNumber
						: (decimal?)null,
			})
			.Where(file => file.ChapterNumber is not null)
			.OrderByDescending(file => file.ChapterNumber)
			.Select(file => new ExistingChapterBoundary(
				file.ChapterNumber!.Value,
				new DateTimeOffset(File.GetLastWriteTimeUtc(file.Path), TimeSpan.Zero)))
			.FirstOrDefault();

	private static string? GetLikelyChapterNumber(
		IReadOnlyList<string> candidates,
		decimal? latestExistingChapter)
	{
		if (candidates.Count == 0)
			return null;
		if (latestExistingChapter is null)
			return candidates[0];

		return candidates
			.Select((value, index) => new
			{
				Value = value,
				Index = index,
				Number = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
					? number
					: (decimal?)null,
			})
			.Where(candidate => candidate.Number is not null)
			.OrderBy(candidate => Math.Abs(candidate.Number!.Value - latestExistingChapter.Value))
			.ThenBy(candidate => candidate.Index)
			.Select(candidate => candidate.Value)
			.FirstOrDefault();
	}

	private static string? GetLikelyNewerChapterNumber(
		IReadOnlyList<string> candidates,
		decimal? latestExistingChapter)
	{
		if (latestExistingChapter is null)
			return candidates.FirstOrDefault();

		return candidates
			.Select((value, index) => new
			{
				Value = value,
				Index = index,
				Number = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
					? number
					: (decimal?)null,
			})
			.Where(candidate => candidate.Number > latestExistingChapter)
			.OrderBy(candidate => candidate.Number)
			.ThenBy(candidate => candidate.Index)
			.Select(candidate => candidate.Value)
			.FirstOrDefault();
	}

	private static string DescribeDiscoveredPost(DiscoveredPost post, IReadOnlyList<string> chapterNumbers)
	{
		if (!string.IsNullOrWhiteSpace(post.Title) && post.Title.Length <= 200)
			return post.Title;

		return chapterNumbers.Count == 0
			? "listing title unavailable"
			: $"chapter candidate(s) {string.Join(", ", chapterNumbers)}";
	}

	private static string? GetChapterNumber(string? title)
	{
		if (string.IsNullOrWhiteSpace(title))
			return null;

		var chapter = ChapterNumberRegex().Match(title).Groups[1].Value;
		if (!string.IsNullOrWhiteSpace(chapter))
			return chapter;

		var bracketed = BracketedChapterNumberRegex().Match(title).Groups[1].Value;
		if (!string.IsNullOrWhiteSpace(bracketed))
			return bracketed;

		var named = NamedChapterNumberRegex().Match(title).Groups[1].Value;
		if (!string.IsNullOrWhiteSpace(named))
			return named;

		var leading = LeadingChapterNumberRegex().Match(title).Groups[1].Value;
		return string.IsNullOrWhiteSpace(leading) ? null : leading;
	}

	private static int TitleScore(string? title)
	{
		if (string.IsNullOrWhiteSpace(title))
			return 0;

		var hasChapterNumber = ChapterNumberRegex().IsMatch(title) ||
			NamedChapterNumberRegex().IsMatch(title) ||
			BracketedChapterNumberRegex().IsMatch(title) ||
			NumberedPostTitleRegex().IsMatch(title) ||
			(title.Length <= 300 && GetChapterNumber(title) is not null);
		return !hasChapterNumber
			? Math.Min(title.Length, 500)
			: 10_000 + Math.Min(title.Length, 500);
	}

	private static DateTimeOffset? ParseListingDate(string? value) =>
		DateTimeOffset.TryParse(
			value,
			CultureInfo.InvariantCulture,
			DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
			out var parsed)
			? parsed
			: null;

	private static async Task RecordSeenAsync(PatreonScrapeState state, string postId, CancellationToken token)
	{
		if (state.Add(postId))
			await state.SaveAsync(token);
	}

	private static string? NormalizePostUrl(string href)
	{
		if (!Uri.TryCreate(href, UriKind.Absolute, out var uri) ||
			!uri.Host.EndsWith("patreon.com", StringComparison.OrdinalIgnoreCase) ||
			!PostPathRegex().IsMatch(uri.AbsolutePath))
		{
			return null;
		}

		return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath.TrimEnd('/')}";
	}

	private static string GetPostId(string postUrl) =>
		PostPathRegex().Match(new Uri(postUrl).AbsolutePath).Groups[1].Value;

	private static async Task<ScrapedPost?> ExtractPostAsync(IPage page, string postUrl)
	{
		await page.WaitForTimeoutAsync(1_500);
		var titleLocator = page.Locator(
			"main h1, article h1, h1, [data-tag='post-title'], [data-testid*='post-title'], main [role='heading']")
			.First;
		if (await titleLocator.CountAsync() == 0)
		{
			var headings = await page.Locator("h1, h2, h3, [role='heading']")
				.EvaluateAllAsync<string[]>(
					"elements => elements.slice(0, 20).map(element => (element.textContent || '').trim().slice(0, 160)).filter(Boolean)");
			throw new InvalidDataException(
				$"No post heading was found at {postUrl}. Current page: {page.Url}. Page title: {await page.TitleAsync()}. " +
				$"Visible headings: {string.Join(" | ", headings)}");
		}

		var title = (await titleLocator.InnerTextAsync()).Trim();
		if (string.IsNullOrWhiteSpace(title))
			throw new InvalidDataException($"The post heading was empty at {postUrl}.");

		ILocator? content = null;
		foreach (var selector in ContentSelectors)
		{
			var candidate = page.Locator(selector).First;
			if (await candidate.CountAsync() == 0)
				continue;

			content = candidate;
			break;
		}

		if (content is null)
		{
			var attributes = await page.Locator("main [data-tag], main [data-testid], main [class]")
				.EvaluateAllAsync<string[]>(
					"""
					elements => [...new Set(elements.flatMap(element => [
						element.getAttribute('data-tag'),
						element.getAttribute('data-testid'),
						element.getAttribute('class')
					]).filter(value => value && /post|content|rich|body/i.test(value)))].slice(0, 40)
					""");
			var paragraphParents = await page.Locator("p")
				.EvaluateAllAsync<string[]>(
					"""
					elements => [...new Set(elements.map(element => element.parentElement).filter(Boolean))]
						.map(element => `${element.tagName.toLowerCase()} class="${element.getAttribute('class') || ''}" data-tag="${element.getAttribute('data-tag') || ''}" paragraphs=${element.querySelectorAll(':scope > p').length} text=${(element.textContent || '').trim().length}`)
						.slice(0, 40)
					""");
			throw new InvalidDataException(
				$"No post content container was found at {postUrl}. Candidate attributes: {string.Join(" | ", attributes)}. " +
				$"Paragraph parents: {string.Join(" | ", paragraphParents)}");
		}

		var contentHtml = await content.EvaluateAsync<string>(
			"""
			element => {
				const tag = element.tagName.toLowerCase();
				const closing = `</${tag}>`;
				const emptyElement = element.cloneNode(false).outerHTML;
				const opening = emptyElement.endsWith(closing)
					? emptyElement.slice(0, -closing.length)
					: emptyElement;
				const children = [...element.childNodes]
					.map(node => node.nodeType === Node.TEXT_NODE
						? (node.textContent || '').trim()
						: node.outerHTML)
					.filter(Boolean);
				return children.length === 0
					? element.outerHTML
					: [opening, ...children.map(child => `    ${child}`), closing].join('\n');
			}
			""");
		if (string.IsNullOrWhiteSpace(contentHtml))
			throw new InvalidDataException($"The post content container was empty at {postUrl}.");
		contentHtml = FormatContentHtml(contentHtml);

		var contentText = await content.InnerTextAsync();
		title = NormalizePostTitle(title, contentText);
		var publishedAt = await FindPublishedAtAsync(page);
		var postId = GetPostId(postUrl);
		return new ScrapedPost(postId, title, contentHtml, publishedAt);
	}

	private static async Task<DateTimeOffset?> FindPublishedAtAsync(IPage page)
	{
		var values = await page.Locator("main time[datetime], article time[datetime], time[datetime]")
			.EvaluateAllAsync<string[]>("elements => elements.map(element => element.getAttribute('datetime')).filter(Boolean)");

		foreach (var value in values)
		{
			if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
				return parsed;
		}

		return null;
	}

	private static string DetermineOutputPath(string directory, string chapterNumber) =>
		Path.Combine(directory, $"{chapterNumber}.html");

	private static string FormatContentHtml(string html)
	{
		const int lineLength = 120;
		var output = new StringBuilder(html.Length + 256);
		var sourceLines = html.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
		foreach (var sourceLine in sourceLines)
		{
			var line = sourceLine.TrimEnd();
			if (line.Length <= lineLength ||
				line.Contains("<pre", StringComparison.OrdinalIgnoreCase) ||
				line.Contains("<textarea", StringComparison.OrdinalIgnoreCase))
			{
				output.AppendLine(line);
				continue;
			}

			var initialIndentLength = line.Length - line.TrimStart().Length;
			var continuationIndent = new string(' ', initialIndentLength + 4);
			var remaining = line;
			while (remaining.Length > lineLength)
			{
				var breakAt = FindHtmlWrapPosition(remaining, lineLength);
				var leadingWhitespace = remaining.Length - remaining.TrimStart().Length;
				if (breakAt <= leadingWhitespace)
					break;

				var next = continuationIndent + remaining[(breakAt + 1)..].TrimStart();
				if (next.Length >= remaining.Length)
					break;

				output.AppendLine(remaining[..breakAt].TrimEnd());
				remaining = next;
			}

			output.AppendLine(remaining);
		}

		return output.ToString().TrimEnd('\r', '\n');
	}

	private static int FindHtmlWrapPosition(string value, int maximum)
	{
		var inTag = false;
		var quote = '\0';
		var lastWhitespace = -1;
		for (var index = 0; index < value.Length && index <= maximum; index++)
		{
			var character = value[index];
			if (inTag)
			{
				if (quote != '\0')
				{
					if (character == quote)
						quote = '\0';
					continue;
				}

				if (character is '\'' or '"')
					quote = character;
				else if (character == '>')
					inTag = false;
				continue;
			}

			if (character == '<')
				inTag = true;
			else if (char.IsWhiteSpace(character))
				lastWhitespace = index;
		}

		return lastWhitespace;
	}

	private static string NormalizePostTitle(string title, string contentText)
	{
		if (ChapterNumberRegex().IsMatch(title))
			return title;

		var chapterNumber = GetChapterNumber(title);
		if (string.IsNullOrWhiteSpace(chapterNumber))
			return title;

		var contentTitle = ContentChapterHeadingRegex().Match(contentText).Groups[1].Value.Trim();
		if (!string.IsNullOrWhiteSpace(contentTitle))
			return $"Chapter {chapterNumber}: {contentTitle}";

		var postTitle = LeadingChapterTitleRegex().Match(title).Groups[2].Value.Trim();
		if (string.IsNullOrWhiteSpace(postTitle))
			postTitle = BracketedChapterTitleRegex().Match(title).Groups[2].Value.Trim();
		if (string.IsNullOrWhiteSpace(postTitle))
			postTitle = NamedChapterTitleRegex().Match(title).Groups[2].Value.Trim();
		return string.IsNullOrWhiteSpace(postTitle)
			? $"Chapter {chapterNumber}"
			: $"Chapter {chapterNumber}: {postTitle}";
	}

	private static void Report(
		PatreonScrapeOptions options,
		string message,
		PatreonSeriesMetadata metadata,
		string? postUrl = null) =>
		options.Progress?.Invoke(new PatreonScrapeProgress(message, metadata.Directory, postUrl));

	private sealed record ScrapedPost(string Id, string Title, string ContentHtml, DateTimeOffset? PublishedAt);

	private sealed record ExistingChapterBoundary(decimal ChapterNumber, DateTimeOffset PublishedAt);

	private sealed record LoginBrowser(string Path, string Channel);

	private sealed record DiscoveredPost(string Url, string? Title, DateTimeOffset? PublishedAt);

	private sealed class ListingPost
	{
		public string Href { get; init; } = string.Empty;

		public string? Title { get; init; }

		public string? PublishedAt { get; init; }
	}

	[GeneratedRegex(@"(?i)\b(?:chapter|chap(?:ter)?\.?|ch\.?)\s*[-#:]*\s*(\d+(?:\.\d+)?)\b")]
	private static partial Regex ChapterNumberRegex();

	[GeneratedRegex(@"(?i)\b(?:session|episode)\s*[-#:]*\s*(\d+(?:\.\d+)?)\b")]
	private static partial Regex NamedChapterNumberRegex();

	[GeneratedRegex(@"(?i)/posts/(?:[^/?#]+-)?(\d+)(?:/)?$")]
	private static partial Regex PostPathRegex();

	[GeneratedRegex(@"(?i)/posts/(.+)-\d+/?$")]
	private static partial Regex PostSlugRegex();

	[GeneratedRegex(@"^\s*(\d+(?:\.\d+)?)\b")]
	private static partial Regex LeadingChapterNumberRegex();

	[GeneratedRegex(@"^\s*(\d+(?:\.\d+)?)\s*[:\-–—]\s*(.+?)\s*$")]
	private static partial Regex LeadingChapterTitleRegex();

	[GeneratedRegex(@"(?i)\[[^\]\r\n]{1,40}\]\s*(\d+(?:\.\d+)?)\s*[.:\-–—]\s*[a-z]")]
	private static partial Regex BracketedChapterNumberRegex();

	[GeneratedRegex(@"(?i)^\s*\[[^\]\r\n]{1,40}\]\s*(\d+(?:\.\d+)?)\s*[.:\-–—]\s*(.+?)\s*$")]
	private static partial Regex BracketedChapterTitleRegex();

	[GeneratedRegex(@"(?i)^\s*(?:session|episode)\s+(\d+(?:\.\d+)?)\s*[.:\-–—]\s*(.+?)\s*$")]
	private static partial Regex NamedChapterTitleRegex();

	[GeneratedRegex(@"(?i)(?<!\d)(\d+(?:\.\d+)?)\s*[-–—]\s*[a-z]")]
	private static partial Regex NumberedPostTitleRegex();

	[GeneratedRegex(@"(?is)^\s*Chapter\s+\d+(?:\.\d+)?\s*[:\-–—]\s*(.*?)(?=\r?\n|\s+PoV\b|$)")]
	private static partial Regex ContentChapterHeadingRegex();

}
