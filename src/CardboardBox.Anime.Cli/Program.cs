using CardboardBox.Anime;
using CardboardBox.Anime.Cli;
using CardboardBox.Anime.Cli.Verbs;
using Serilog;

var config = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", false, true)
	.AddEnvironmentVariables()
	.Build();

return await new ServiceCollection()
	.AddLogging(c =>
		c.AddSerilog(new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File(Path.Combine("logs", "log.txt"), rollingInterval: RollingInterval.Day)
			.CreateLogger())
	)
	.AddSingleton<IConfiguration>(config)
	.RegisterCba(config)
    .AddSingleton<IRunner, Runner>()

    .Cli(args, c => c
		.Add<RunnerVerb>()
		.Add<LoadNovelVerb>()
		.Add<NovelEPubVerb>());

	