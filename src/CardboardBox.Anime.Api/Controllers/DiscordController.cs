using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers;

using Database;
using DiscordIntermediary;

[ApiController]
public class DiscordController : ControllerBase
{
	private readonly IDiscordGuildDbService _discord;
	private readonly IDiscordLogDbService _logs;
	private readonly IDiscordClient _client;

	public DiscordController(
		IDiscordGuildDbService discord,
		IDiscordLogDbService logs,
		IDiscordClient client)
	{
		_discord = discord;
		_client = client;
		_logs = logs;
	}

	[HttpGet, Route("discord/settings")]
	[ProducesDefaultResponseType(typeof(DbDiscordGuildSettings[]))]
	public async Task<IActionResult> Get()
	{
		var records = await _discord.All();
		return Ok(records);
	}

	[HttpPost, Route("discord/message")]
	[ProducesDefaultResponseType(typeof(DbDiscordLog))]
	public async Task<IActionResult> CreateMessage([FromBody] DbDiscordLog log)
	{
		log.Id = await _logs.Insert(log);
		return Ok(log);
	}

	[HttpDelete, Route("discord/message/{messageId}")]
    [ProducesDefaultResponseType(typeof(DeleteCount))]
    public async Task<IActionResult> DeleteMessage([FromRoute] string messageId)
	{
		var res = await _logs.Delete(messageId);
		return Ok(res);
	}

	[HttpGet, Route("discord/settings/{guildId}")]
	[ProducesDefaultResponseType(typeof(DbDiscordGuildSettings))]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Get(string guildId)
	{
		var data = await _discord.Get(guildId);
		if (data == null) return NotFound();

		return Ok(data);
	}

	[HttpPost, Route("discord/settings"), AdminAuthorize]
	[ProducesDefaultResponseType(typeof(DbDiscordGuildSettings))]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Post([FromBody] DbDiscordGuildSettings settings)
	{
		var id = await _discord.Upsert(settings);
		var data = await _discord.Fetch(id);
		if (data == null) return NotFound();

		return Ok(data);
	}

	[HttpGet, Route("discord/guilds")]
	[ProducesDefaultResponseType(typeof(FakeGuild[]))]
	public async Task<IActionResult> Guilds()
	{
		var guilds = await _client.GetGuilds();
		return Ok(guilds);
	}

	[HttpGet, Route("discord/guilds/{id}")]
	[ProducesDefaultResponseType(typeof(FakeGuild))]
	public async Task<IActionResult> Guilds(ulong id)
	{
		return Ok(await _client.GetGuild(id));
	}

	[HttpGet, Route("discord/user/{id}")]
	[ProducesDefaultResponseType(typeof(FakeUser))]
	public async Task<IActionResult> GetUser(ulong id)
	{
		return Ok(await _client.GetUser(id));
	}

	[HttpGet, Route("discord/guild/{guildId}/user/{id}")]
	[ProducesDefaultResponseType(typeof(FakeGuildUser))]
	public async Task<IActionResult> GetUser(ulong guildId, ulong id)
	{
		return Ok(await _client.GetUser(id, guildId));
	}
}
