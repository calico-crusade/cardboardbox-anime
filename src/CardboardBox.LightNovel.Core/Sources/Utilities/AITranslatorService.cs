using CardboardBox.Anime.ChatGPT;

namespace CardboardBox.LightNovel.Core.Sources.Utilities;

public interface IAITranslatorService
{
    Task<(string? result, GptResponse.GptUsage tokens, string? error)> Translate(string source, string? language = null, string? promptAddition = null);
}

internal class AITranslatorService(
    IConfiguration _config,
    IChatGptService _ai) : IAITranslatorService
{
    private const string DEFAULT_LANGUAGE = "Japanese";
    private const string DEFAULT_SYSTEM_PROMPT = @"You are a professional book translator.
You will not say anything other than the translated text.
You will preserve the formatting of the article using Markdown.";
    private const string DEFAULT_PROMPT = @"{0}
Please translate the following text from {1} to English:
{2}";
    private string DefaultTranslationPrompt => _config["ChatGPT:TranslationPrompt"] ?? DEFAULT_PROMPT;
    private string DefaultSystemPrompt => _config["ChatGPT:SystemPrompt"] ?? DEFAULT_SYSTEM_PROMPT;

    private string TranslationModel => _config["ChatGPT:TranslationModel"] ?? "gpt-4o";

    public async Task<(string? result, GptResponse.GptUsage tokens, string? error)> Translate(string source, string? language = null, string? promptAddition = null)
    {
        const int gtp4oMaxLength = 128_000;
        var prompt = string.Format(DefaultTranslationPrompt, promptAddition ?? "", language ?? DEFAULT_LANGUAGE, source);
        var request = new GptChat
        {
            Model = TranslationModel,
            Messages = 
            [
                GptMessage.System(DefaultSystemPrompt),
                GptMessage.User(prompt)
            ]
        };
        var tokens = _ai.CountTokens(request);
        var usage = new GptResponse.GptUsage
        {
            PromptTokens = tokens,
            CompletionTokens = 0,
            TotalTokens = tokens
        };
        if (tokens >= gtp4oMaxLength)
            return (null, usage, "Too many tokens");
        var response = await _ai.Completions(request, gtp4oMaxLength);
        if (response == null) 
            return (null, usage, "No response found");

        if (response.Choices is null || response.Choices.Length == 0)
            return (null, response.Usage ?? usage, "No choices present");

        var choice = response.Choices.First();
        return (choice.Message.Content, response.Usage ?? usage, null);
    }
}
