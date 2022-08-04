using CardboardBox.Anime.Cli;
using CardboardBox.Anime.Vrv;
using CardboardBox.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var config = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", false, true)
	.AddEnvironmentVariables()
	.Build();

return await new ServiceCollection()
	.AddCardboardHttp()
	.AddLogging(c =>
		c.AddSerilog(new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File(Path.Combine("logs", "log.txt"), rollingInterval: RollingInterval.Day)
			.CreateLogger())
	)
	.AddSingleton<IConfiguration>(config)

	.AddTransient<IVrvApiService, VrvApiService>()

	.AddSingleton<IRunner, Runner>()
	.BuildServiceProvider()
	.GetRequiredService<IRunner>()
	.Run(Environment.GetCommandLineArgs());

	