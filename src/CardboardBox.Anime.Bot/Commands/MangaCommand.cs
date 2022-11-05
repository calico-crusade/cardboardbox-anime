using CardboardBox.Discord;
using CardboardBox.Discord.Components;
using CardboardBox.Manga;
using Discord;
using Discord.WebSocket;

namespace CardboardBox.Anime.Bot.Commands
{
	public class MangaCommand
	{
		private readonly IComponentService _components;
		private readonly IMangaService _manga;

		public MangaCommand(
			IComponentService components,
			IMangaService manga)
		{
			_components = components;
			_manga = manga;
		}

		[Command("manga", "Allows reading a managa on discord")]
		public async Task Manga(SocketSlashCommand cmd, [Option("Manga URL", true)] string url)
		{
			var (src, id) = _manga.DetermineSource(url);
			if (src == null)
			{
				await cmd.Respond($"We don't support that site yet. We only support:\r\n{string.Join("\r\n", _manga.Sources().Select(t => t.HomeUrl))}", ephemeral: true);
				return;
			}

			var comp = await _components.Components<MangaComponent>(cmd);
			await cmd.Respond("One sec while I load some stuff!", components: comp);
		}
	}

	public class MangaComponent : ComponentHandler
	{
		private readonly IComponentService _components;
		private readonly IMangaService _manga;

		public MangaComponent(
			IComponentService components, 
			IMangaService manga)
		{
			_components = components;
			_manga = manga;
		}

		[Button("Next", "👉", ButtonStyle.Primary)]
		public async Task Next()
		{
			var interaction = Message?.Interaction as IApplicationCommand;

		}
	}
}
