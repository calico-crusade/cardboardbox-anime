using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers;

using Auth;
using ChatGPT;

[ApiController, Authorize]
public class ChatController : ControllerBase
{
	private readonly ILogger _logger;
	private readonly IChatGptService _chat;

	public ChatController(ILogger<ChatController> logger, IChatGptService chat)
	{
		_logger = logger;
		_chat = chat;
	}

	[HttpPost, Route("chat")]
	public async Task<IActionResult> Chat([FromBody] GptMessage[] messages)
	{
		var user = this.UserFromIdentity();
		if (user == null) return Unauthorized();

		var chat = new GptChat
		{
			Messages = messages.ToList()
		};

		if (chat.Messages.Count == 0) return BadRequest();

		var tokens = _chat.CountTokens(chat);
		if (tokens > 4096)
			return BadRequest();

		var resp = await _chat.Completions(chat);
		if (resp == null ||
			resp.Choices.Length == 0)
			return NotFound();

		_logger.LogInformation("[CHATGPT REPORT] Usage: Prompt {0} - Compeltion {1} - Total {2} >> {3} ({4}): \"{5}\"",
			resp.Usage.PromptTokens, resp.Usage.CompletionTokens, resp.Usage.TotalTokens,
			user.Nickname, user.Id, chat.Messages.Last().Content);

		return Ok(new
		{
			message = resp.Choices.First().Message,
			usage = resp.Usage
		});
	}
}
