using CardboardBox.Anime;
using CardboardBox.Anime.Cli;
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
	.BuildServiceProvider()
	.GetRequiredService<IRunner>()
	.Run(Environment.GetCommandLineArgs());

	