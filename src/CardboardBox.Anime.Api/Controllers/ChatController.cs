using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Api.Controllers;

using Auth;
using ChatGPT;
using Database;

[ApiController, Authorize]
public class ChatController : ControllerBase
{
	private readonly ILogger _logger;
	private readonly IChatGptService _chat;
	private readonly IDbService _db;

	public ChatController(ILogger<ChatController> logger, IChatGptService chat, IDbService db)
	{
		_logger = logger;
		_chat = chat;
		_db = db;
	}

	[HttpGet, Route("chat")]
	[ProducesDefaultResponseType(typeof(DbChat[]))]
	public async Task<IActionResult> Get()
	{
		var id = this.UserFromIdentity()?.Id;
		if (string.IsNullOrEmpty(id)) return Unauthorized();

		var chats = await _db.Chat.Chats(id);
		return Ok(chats);
	}

	[HttpGet, Route("chat/{id}")]
	[ProducesDefaultResponseType(typeof(DbChatData))]
	public async Task<IActionResult> Get([FromRoute] long id)
	{
		var pid = this.UserFromIdentity()?.Id;
		if (string.IsNullOrEmpty(pid)) return Unauthorized();

		var profile = await _db.Profiles.Fetch(pid);
		if (profile == null) return Unauthorized();

		var chat = await _db.Chat.Chat(id);
		if (chat == null) return NotFound();

		if (chat.Chat.ProfileId != profile.Id) return NotFound();

		return Ok(chat);
	}

	[HttpPost, Route("chat")]
	public async Task<IActionResult> New([FromBody] ChatRequest chat)
	{
		var id = this.UserFromIdentity()?.Id;
		if (string.IsNullOrEmpty(id)) return Unauthorized();

		var profile = await _db.Profiles.Fetch(id);
		if (profile == null) return Unauthorized();

		var chatId = await _db.Chat.Insert(new DbChat
		{
			ProfileId = profile.Id,
			Status = DbChatStatus.OnGoing,
			Grounder = chat.Content
		});
		return Ok(new { id = chatId });
	}

	[HttpPost, Route("chat/{chatId}")]
	[ProducesDefaultResponseType(typeof(ChatResponse))]
	public async Task<IActionResult> Chat([FromRoute] long chatId, [FromBody] ChatRequest message)
	{
		if (string.IsNullOrEmpty(message.Content)) return BadRequest();

		var user = this.UserFromIdentity();
		var id = user?.Id;
		if (user == null || string.IsNullOrEmpty(id)) return Unauthorized();

		var profile = await _db.Profiles.Fetch(id);
		if (profile == null) return Unauthorized();

		var chat = await _db.Chat.Chat(chatId);
		if (chat == null) return NotFound();
		if (chat.Chat.ProfileId != profile.Id) return Unauthorized();
		if (chat.Chat.Status != DbChatStatus.OnGoing) return UnprocessableEntity();

		var msg = new DbChatMessage
		{
			ChatId = chat.Chat.Id,
			ProfileId = profile.Id,
			Content = message.Content,
			Type = DbMessageType.User,
			ImageId = null
		};

		msg.Id = await _db.Chat.Insert(msg);

		var gptChat = ToChat(chat, msg);
		var tokens = _chat.CountTokens(gptChat);
		if (tokens > 4096)
		{
			chat.Chat.Status = DbChatStatus.ReachedLimit;
			await _db.Chat.Update(chat.Chat);
			return Ok(new ChatResponse(1, "Token limit reached. Chat has been closed."));
		}

		var resp = await _chat.Completions(gptChat);
		if (resp == null || resp.Choices.Length == 0)
		{
			chat.Chat.Status = DbChatStatus.ErrorOccurred;
			await _db.Chat.Update(chat.Chat);
			return Ok(new ChatResponse(2, "An error occurred. There was no response from ChatGPT."));
		}

		_logger.LogInformation("[CHATGPT REPORT] Usage: Prompt {0} - Compeltion {1} - Total {2} >> {3} ({4}): \"{5}\"",
			resp.Usage.PromptTokens, resp.Usage.CompletionTokens, resp.Usage.TotalTokens,
			user.Nickname, user.Id, gptChat.Messages.Last().Content);

		var response = resp.Choices.First().Message.Content;
		await _db.Chat.Insert(new DbChatMessage
		{
			Content = response,
			Type = DbMessageType.Bot,
			ChatId = chat.Chat.Id,
			ProfileId = null,
			ImageId = null,
		});

		return Ok(new ChatResponse(0, response));
	}

	private static GptChat ToChat(DbChatData chat, params DbChatMessage[] messages)
	{
		var output = new GptChat();

		if (!string.IsNullOrEmpty(chat.Chat.Grounder))
			output.Messages.Add(GptMessage.System(chat.Chat.Grounder));

		foreach(var msg in chat.Messages.Concat(messages))
		{
			if (msg.Type == DbMessageType.Image) continue;

			output.Messages.Add(msg.Type == DbMessageType.User ? 
				GptMessage.User(msg.Content) : 
				GptMessage.Assistant(msg.Content));
		}

		return output;
	}

	public class ChatRequest
	{
		[JsonPropertyName("content")]
		public string? Content { get; set; }
	}

	public class ChatResponse
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("worked")]
		public bool Worked { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; } = string.Empty;

		public ChatResponse() { }

		public ChatResponse(int code, string message)
		{
			Code = code;
			Message = message;
			Worked = code == 0;
		}
	}
}
