﻿using AI.Dev.OpenAI.GPT;
using System.Net.Http.Headers;

namespace CardboardBox.Anime.ChatGPT;

public interface IChatGptService
{
	int CountTokens(GptChat chat);
	List<int> Tokenize(string message);
	Task<GptResponse?> Completions(GptChat chat, int? maxTokenSize = null);
}

public class ChatGptService : IChatGptService
{
	private readonly IApiService _api;
	private readonly IConfiguration _config;

	private string ApiToken => _config["ChatGPT:Token"] ?? throw new ArgumentNullException("ChatGPT:Token", "ChatGPT token is missing from config");
	private int MaxTokenSize => int.TryParse(_config["ChatGPT:MaxTokenSize"], out var size) ? size : 4096;

    public ChatGptService(
		IApiService api, 
		IConfiguration config)
	{
		_api = api;
		_config = config;
	}

	public int CountTokens(GptChat chat)
	{
		int tokens = 2;

		foreach(var msg in chat.Messages)
			tokens += 4 + Tokenize(msg.Role).Count + Tokenize(msg.Content).Count;

		return tokens;
	}

	public List<int> Tokenize(string message) =>  GPT3Tokenizer.Encode(message);

	public Task<GptResponse?> Completions(GptChat chat, int? maxTokenSize = null)
	{
		maxTokenSize ??= MaxTokenSize;

        var tokenize = CountTokens(chat);
		if (tokenize > maxTokenSize) throw new ArgumentException($"Token count exceeds limit: {maxTokenSize} < {tokenize}", nameof(chat));

		return _api.Post<GptResponse, GptChat>("https://api.openai.com/v1/chat/completions", chat, c =>
		{
			c.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);
		});
	}
}