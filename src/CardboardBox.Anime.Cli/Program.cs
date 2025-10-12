using CardboardBox.Anime;
using CardboardBox.Anime.Cli;
using CardboardBox.Anime.Cli.Interactive;
using CardboardBox.Anime.Cli.Verbs;
using Serilog;

var config = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", false, true)
	.AddEnvironmentVariables()
	.Build();

var logConfig = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.File(Path.Combine("logs", "log.txt"), rollingInterval: RollingInterval.Day)
	.WriteTo.Sink(new EventSink(), Serilog.Events.LogEventLevel.Verbose);

if (!args.Any(t => t.Contains("interactive", StringComparison.OrdinalIgnoreCase)))
	logConfig.WriteTo.Console();

return await new ServiceCollection()
	.AddLogging(c => c.AddSerilog(logConfig.CreateLogger()))
	.AddSingleton<IConfiguration>(config)
	.RegisterCba(config)
    .AddSingleton<IRunner, Runner>()

    .Cli(args, c => c
		.Add<RunnerVerb>()
		.Add<LoadNovelVerb>()
		.Add<NovelEPubVerb>()
		.Add<ManualLoadVerb>()
		.Add<InteractiveVerb>()
		.Add<MaxLevelPreistessVerb>()
		.Add<RepurgeVerb>());