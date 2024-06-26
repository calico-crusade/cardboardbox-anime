﻿using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers;

using Database;
using DiscordIntermediary;

[ApiController]
public class DiscordController : ControllerBase
{
	private readonly IDiscordGuildDbService _discord;
	private readonly IDiscordClient _client;

	public DiscordController(
		IDiscordGuildDbService discord,
		IDiscordClient client)
	{
		_discord = discord;
		_client = client;
	}

	[HttpGet, Route("discord/settings")]
	[ProducesDefaultResponseType(typeof(DbDiscordGuildSettings[]))]
	public async Task<IActionResult> Get()
	{
		var records = await _discord.All();
		return Ok(records);
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
