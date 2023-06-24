using CardboardBox.Anime.AI;

namespace CardboardBox.Anime.Bot.Commands;

public class AiCommands
{
    private readonly IComponentService _components;
    private readonly ICommandStateService _state;
    private readonly IAiAnimeService _ai;

    public AiCommands(
        IComponentService components, 
        ICommandStateService state,
        IAiAnimeService ai)
    {
        _components = components;
        _state = state;
        _ai = ai;
    }

    [Command("ai-embeds", "Displays a list of all of the embeds loaded on the system", LongRunning = true)]
    public async Task EmbeddingList(SocketSlashCommand cmd)
    {
        var emebds = await _ai.Embeddings();
        if (emebds.Length == 0)
        {
            await cmd.Modify("I couldn't find any embeds! Maybe the API is dead?");
            return;
        }

        await cmd.Modify(
            "These are all of the embeddings I found, you can put them in prompts and it modifies what the image looks like:\r\n" +
            string.Join(", ", emebds));
    }

    [Command("ai-loras", "Displays a list of all loaded LORA models", LongRunning = true)]
    public async Task Loras(SocketSlashCommand cmd)
    {
        var loras = await _ai.Loras();
        if (loras == null || loras.Length == 0)
        {
            await cmd.Modify("I couldn't find any LORA models! Maybe the API is dead?");
            return;
        }

        await cmd.Modify(
            "These are all of the LORA models I found. You can put them in prompts and it will change how the image looks:\r\n\r\n* " +
            string.Join("\r\n* ", loras.Select(t => $"{t.Name} ({t.Alias})")) + "\r\n\r\n" +
            "You include them by adding `<lora:{name/alias}:{strength}>` to the prompt\r\n" +
            "Example: `1girl, fran, cat tail, cat ears, black hair, blue eyes, <lora:FranRAAS:0.9>`\r\n" +
            "Found an interesting model? You can ask [Cardboard](<https://discord.com/users/191100926486904833>) to load it! You can browse models on: <https://civitai.com>");
    }

    [Command("ai", "Generates an image with the given data", LongRunning = true)]
    public async Task Ai(SocketSlashCommand cmd,
        [Option("Generation Prompt", true)] string prompt,
        [Option("Negative Generation Prompt", false)] string? negativePrompt,
        [Option("Generation Steps (1 - 64)", false)] long? steps,
        [Option("CFG Scale (1 - 30)", false)] double? cfg,
        [Option("Generation Seed (1+)", false)] string? seed)
    {
        steps ??= AiState.DEFAULT_STEPS;
        cfg ??= AiState.DEFAULT_CFG;
        seed ??= AiState.DEFAULT_SEED;

        var state = new AiState(prompt)
        {
            NegativePrompt = negativePrompt,
            Steps = steps,
            Cfg = cfg,
            Seed = seed
        };

        var validate = state.Validate().ToArray();
        if (validate.Length > 0)
        {
            await cmd.Modify("There were some issues with your input:\r\n" + string.Join("\r\n", validate));
            return;
        }

        var comp = await _components.Components<AiComponent>(cmd);
        var msg = await cmd.ModifyOriginalResponseAsync(t =>
        {
            t.Embed = state.Generate().Build();
            t.Components = comp;
        });
        _state.Set(msg, state);
    }

    [Command("ai-img", "Generates an image with the given data", LongRunning = true)]
    public async Task Img2ImgAi(SocketSlashCommand cmd,
        [Option("Image Url", true)] string imageUrl,
        [Option("Generation Prompt", true)] string prompt,
        [Option("Negative Generation Prompt", false)] string? negativePrompt,
        [Option("Generation Steps (1 - 64)", false)] long? steps,
        [Option("CFG Scale (1 - 30)", false)] double? cfg,
        [Option("Generation Seed (1+)", false)] string? seed,
        [Option("Denoise Strength (0.0 - 1.0)", false)] double? denoiseStrength)
    {
        steps ??= AiState.DEFAULT_STEPS;
        cfg ??= AiState.DEFAULT_CFG;
        seed ??= AiState.DEFAULT_SEED;

        var state = new AiState(prompt)
        {
            NegativePrompt = negativePrompt,
            Steps = steps,
            Cfg = cfg,
            Seed = seed,
            ImageUrl = imageUrl,
            DenoiseStrength = denoiseStrength
        };

        var validate = state.Validate().ToArray();
        if (validate.Length > 0)
        {
            await cmd.Modify("There were some issues with your input:\r\n" + string.Join("\r\n", validate));
            return;
        }

        var comp = await _components.Components<AiComponent>(cmd);
        var msg = await cmd.ModifyOriginalResponseAsync(t =>
        {
            t.Embed = state.Generate().Build();
            t.Components = comp;
        });
        _state.Set(msg, state);
    }
}

public class AiComponent : ComponentHandler
{
    private static LoraResponse[]? _loras;
    private static SamplerResponse[]? _samplers;

    private readonly IAiAnimeService _ai;
    private readonly IComponentService _comp;
    private readonly ICommandStateService _state;

    public IMessage ActualMessage => Message ?? throw new ArgumentNullException(nameof(Message));

    public AiComponent(
        IAiAnimeService ai, 
        IComponentService comp, 
        ICommandStateService state)
    {
        _ai = ai;
        _comp = comp;
        _state = state;
    }

    public async Task<(bool, AiState?)> Validate(bool sameUser = true)
    {
        if (sameUser && Message?.Interaction.User.Id != User?.Id)
        {
            await Acknowledge();
            return (false, null);
        }

        return await _state.ValidateState<AiState>(this);
    }

    public Task Update(Action<MessageProperties> action, bool isResponse)
    {
        return !isResponse && ActualMessage is SocketUserMessage cmd
            ? cmd.ModifyAsync(action)
            : base.Update(action);
    }

    public async Task Update(AiState state, string? error = null, string? message = null, bool isResponse = false)
    {
        var comps = string.IsNullOrEmpty(error) ? await _comp.Components<AiComponent>(ActualMessage) : null;
        var embed = state.Generate(error);

        await Update(t =>
        {
            t.Embed = embed.Build();
            t.Components = comps;
            t.Content = message;
        }, true);

        if (!string.IsNullOrEmpty(error))
            _state.Remove(ActualMessage);
    }

    public async Task Loading(AiState state)
    {
        if (ActualMessage is not SocketUserMessage cmd)
            return;

        await Update(a =>
        {
            a.Components = null;
            a.Content = "<a:loading:1048471999065903244> Loading...";
            a.Embed = state.Generate().Build();
        }, true);
    }

    public async Task Finished(AiState state, string[] images)
    {
        var imgs = await images.Select(async t =>
        {
            var temp = await _ai.DecodeAndSave(t, "discord-ai-images");
            var attach = new FileAttachment(temp);
            return (temp, attach);
        }).WhenAll();

        await Update(t =>
        {
            t.Content = "Here you go!";
            t.Embed = state.Generate().Build();
            t.Components = null;
            t.Attachments = imgs.Select(t => t.attach).ToArray();
        }, false);

        foreach (var (temp, _) in imgs)
            File.Delete(temp);

        _state.Remove(ActualMessage);
    }

    public async Task<SelectMenuOptionBuilder[]> Models()
    {
        var loras = _loras ??= await _ai.Loras();
        return loras?
            .Select(t => new SelectMenuOptionBuilder(t.Name, t.Name, $"Alias: {t.Alias}"))
            .ToArray() 
            ?? new[] {
                new SelectMenuOptionBuilder("No Models found", "", "I couldn't find any LORA models. Contact an admin!")
            };
    }

    public async Task<SelectMenuOptionBuilder[]> Samplers()
    {
        var samplers = _samplers ??= await _ai.Samplers();
        return samplers?
            .Select(t => new SelectMenuOptionBuilder(t.Name, t.Name))
            .ToArray()
            ?? new[] {
                new SelectMenuOptionBuilder("No Models found", "", "I couldn't find any samplers. Contact an admin!")
            };
    }

    public Task<SelectMenuOptionBuilder[]> Sizes()
    {
        var sizes = new[] { 128, 256, 512, 768, 1024 };

        var profile = (int w, int h) => {
            var style = w == h ? "Square" : w > h ? "Landscape" : "Portrait";
            var emote = w == h ? "🔲" : w > h ? "⛰️" : "🤳";
            return new ImageSize(w, h, $"{w} x {h}", style, emote);
        };

        var imageSizes = sizes.SelectMany(w => sizes.Select(h => profile(w, h))).ToArray();
        return Task.FromResult(
            imageSizes
                .OrderBy(t => t.Style)
                .Select(t => new SelectMenuOptionBuilder(t.Display, t.Display, t.Style, Emoji.Parse(t.Emote)))
                .ToArray());
    }

    public Task<SelectMenuOptionBuilder[]> LoraSize()
    {
        var sizes = new List<(float value, string desc)>();
        for(float size = 0.05f; size < 1.05; size += 0.05f)
            sizes.Add((size, 
                size < 0.3 
                    ? "Barely Impactful" 
                    : size < 0.55 
                        ? "Some-what Impactful" 
                        : size < 0.80 
                            ? "Decently Impactful" 
                            : size < 1 
                                ? "Very Impactful"
                                : "The most Impactful"));

        return Task.FromResult(
            sizes
                .Select(t => new SelectMenuOptionBuilder($"{t.value:N2} strength", t.value.ToString("N2"), t.desc))
                .ToArray());
    }

    [SelectMenu(nameof(Models), Placeholder = "LORA Model", Row = 1)]
    public async Task SelectLora()
    {
        var (worked, state) = await Validate();
        if (!worked || state == null) return;

        state.Lora = Value;
        await Update(state, isResponse: true);
    }

    [SelectMenu(nameof(LoraSize), Placeholder = "LORA Impact", Row = 1)]
    public async Task SelectLoraSize()
    {
        var (worked, state) = await Validate();
        if (!worked || state == null) return;

        state.LoraStrength = double.TryParse(Value, out var val) ? val : 1;
        await Update(state, isResponse: true);
    }

    [SelectMenu(nameof(Samplers), Placeholder = "Sampling Method", Row = 2)]
    public async Task SelectSampler()
    {
        var (worked, state) = await Validate();
        if (!worked || state == null) return;

        state.Sampler = Value;
        await Update(state, isResponse: true);
    }

    [SelectMenu(nameof(Sizes), Placeholder = "Image Size", Row = 3)]
    public async Task SelectSize()
    {
        var (worked, state) = await Validate();
        if (!worked || state == null) return;

        var split = Value?.Split('x') ?? Array.Empty<string>();
        state.Width = split.Length == 2 ? int.Parse(split[0].Trim()) : AiState.DEFAULT_SIZE;
        state.Height = split.Length == 2 ? int.Parse(split[1].Trim()) : AiState.DEFAULT_SIZE;
        await Update(state, isResponse: true);
    }

    [Button("Generate", "<a:dancesaber:1114035664472768542>", ButtonStyle.Success, Row = 4)]
    public async Task Generate()
    {
        var (worked, state) = await Validate();
        if (!worked || state == null) return;

        var valids = state.Validate().ToArray();
        if (valids.Length != 0)
        {
            await Update(state, "There was a problem: \r\n" + string.Join("\n", valids), isResponse: true);
            return;
        }

        await Loading(state);

        var req = state.ToRequest();
        var resp = await (req is AiRequestImg2Img img ? _ai.Img2Img(img) : _ai.Text2Img(req));

        if (resp == null || resp.Images == null || resp.Images.Length == 0)
        {
            await Update(state, "There was a problem generating the image.");
            return;
        }

        await Finished(state, resp.Images);
    }

    [Button("Cancel", "❌", ButtonStyle.Danger, Row = 4)]
    public async Task Cancel()
    {
        var (worked, state) = await Validate();
        if (!worked || state == null) return;

        _state.Remove(ActualMessage);
        await RemoveComponents(t =>
        {
            t.Content = "Request Canceled.";
        });
    }

    public record class ImageSize(int Width, int Height, string Display, string Style, string Emote);
}

public class AiState
{
    public const long DEFAULT_STEPS = 20, DEFAULT_SIZE = 512;
    public const double DEFAULT_CFG = 7, DEFAULT_DENOISE = 0.7;
    public const string DEFAULT_SEED = "-1";

    public string Prompt { get; set; }

    public string? NegativePrompt { get; set; }
    public string? ImageUrl { get; set; }
    public double? DenoiseStrength { get; set; }
    public long? Steps { get; set; }
    public double? Cfg { get; set; }
    public string? Seed { get; set; }
    public long? Width { get; set; }
    public long? Height { get; set; }
    public string? Lora { get; set; }
    public double? LoraStrength { get; set; }
    public string? Sampler { get; set; }

    public AiState(string prompt)
    {
        Prompt = prompt;
    }
    
    public IEnumerable<string> Validate()
    {
        NegativePrompt ??= "";
        Steps ??= DEFAULT_STEPS;
        Cfg ??= DEFAULT_CFG;
        Seed ??= DEFAULT_SEED;
        Width ??= DEFAULT_SIZE;
        Height ??= DEFAULT_SIZE;

        if (Width < 100 || Width > 1024 || Height < 100 || Height > 1024)
            yield return "Image size has to be within 100x100 and 1024x1024!";

        if (Steps < 1 || Steps > 64)
            yield return "Generation Steps has to be between 1 and 64";

        if (Cfg < 1 || Cfg > 30)
            yield return "CFG has to be between 1 and 30";

        if (!long.TryParse(Seed, out long actualSeed) || (actualSeed < 1 && actualSeed != -1))
            yield return "Seed has to be a number and cannot be less than 1!";
    }

    public AiRequest ToRequest()
    {
        if (!long.TryParse(Seed, out long actualSeed) || (actualSeed < 1 && actualSeed != -1))
            actualSeed = -1;

        var prompt = Prompt;
        if (!string.IsNullOrEmpty(Lora))
            prompt += $", <lora:{Lora}:{LoraStrength ?? 1}>";

        var request = string.IsNullOrEmpty(ImageUrl) ? new AiRequest() : new AiRequestImg2Img
        {
            Images = new[] { ImageUrl },
            DenoiseStrength = DenoiseStrength ?? DEFAULT_DENOISE
        };

        request.Prompt = prompt;
        request.NegativePrompt = NegativePrompt ?? "";
        request.Steps = Steps ?? DEFAULT_STEPS;
        request.CfgScale = Cfg ?? DEFAULT_CFG;
        request.BatchCount = 1;
        request.BatchSize = 1;
        request.Seed = actualSeed;
        request.Width = Width ?? DEFAULT_SIZE;
        request.Height = Height ?? DEFAULT_SIZE;
        request.Sampler = Sampler;

        return request;
    }

    public EmbedBuilder Generate(string? error = null)
    {
        var desc = $"Prompt:\r\n```\r\n{Prompt}\r\n```";
        if (!string.IsNullOrWhiteSpace(NegativePrompt))
        {
            desc += $"\r\nNegative Prompt:\r\n```\r\n{NegativePrompt}\r\n```";
        }

        var e = new EmbedBuilder()
            .WithTitle("AI Image Generation Prompt")
            .WithDescription(desc);

        if (!string.IsNullOrWhiteSpace(ImageUrl))
            e.WithImageUrl(ImageUrl);

        if (DenoiseStrength != null)
            e.AddField("Denoise Strength", DenoiseStrength.Value.ToString("N2"), true);

        if (Steps != null)
            e.AddField("Steps", Steps.Value.ToString("N0"), true);

        if (Cfg != null)
            e.AddField("Cfg", Cfg.Value.ToString("N2"), true);

        if (!string.IsNullOrWhiteSpace(Seed))
            e.AddField("Seed", Seed, true);

        if (Width != null && Height != null)
            e.AddField("Size", $"{Width:N0}x{Height:N0}", true);
        if (!string.IsNullOrEmpty(Sampler))
            e.AddField("Sampler", Sampler, true);
        if (!string.IsNullOrEmpty(Lora))
            e.AddField("LORA Model", Lora, true);
        if (LoraStrength != null)
            e.AddField("LORA Strength", LoraStrength.Value.ToString("N2"), true);

        if (!string.IsNullOrEmpty(error))
            e.WithColor(Color.Red).AddField("Error: ", error);

        return e;
    }
}