using CardboardBox.Anime;
using CardboardBox.BackgroundTasks.Cli.Tasks;
using Serilog;

var config = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", false, true)
	.AddEnvironmentVariables()
	.Build();

var services = new ServiceCollection()
	.AddLogging(c =>
		c.AddSerilog(new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File(Path.Combine("logs", "log.txt"), rollingInterval: RollingInterval.Day)
			.CreateLogger())
	)
	.AddSingleton<IConfiguration>(config)

	.RegisterCba(config)

	.AddSingleton<IScheduledTask, ReverseImageSearchIndexing>()
	.AddSingleton<IScheduledTask, MangaUpdater>()
	.BuildServiceProvider();

var tasks = services.GetServices<IScheduledTask>();

foreach (var task in tasks)
	_ = Task.Run(async () =>
	{
		while (true)
		{
			await task.Run();
			if (task.DelayMs > 0)
				await Task.Delay(task.DelayMs);
		}
	});

await Task.Delay(-1);