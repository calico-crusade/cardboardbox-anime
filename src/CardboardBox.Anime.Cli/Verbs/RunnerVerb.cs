using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("runner", HelpText = "Runs a test in the old runner service")]
public class RunnerOptions
{
    [Value(0, HelpText = "The test to run", Required = false, Default = null)]
    public string? Value { get; set; }
}

internal class RunnerVerb(
    ILogger<RunnerVerb> _logger, 
    IRunner _runner) : BooleanVerb<RunnerOptions>(_logger)
{
    public override async Task<bool> Execute(RunnerOptions options, CancellationToken token)
    {
        var args = options.Value?.Split(' ') ?? [];
        return await _runner.Run(args) == 0;
    }
}
