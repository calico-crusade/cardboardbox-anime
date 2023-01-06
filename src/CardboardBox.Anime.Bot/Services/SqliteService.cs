using System.Data;
using Microsoft.Data.Sqlite;

namespace CardboardBox.Anime.Bot.Services
{
	public class SqliteService : SqlService
	{
		private readonly IConfiguration _config;
		private readonly ILogger _logger;

		public override int Timeout => 0;
		public bool FirstRun { get; private set; } = true;

		public SqliteService(
			IConfiguration config,
			ILogger<SqliteService> logger)
		{
			_config = config;
			_logger = logger;
		}

		public override IDbConnection CreateConnection()
		{
			var constring = _config["Sqlite:ConnectionString"];
			var con = new SqliteConnection(constring);
			con.Open();

			if (!FirstRun) return con;

			FirstRun = false;
			ExecuteScripts(con).Wait();
			return con;
		}

		public async Task ExecuteScripts(IDbConnection con)
		{
			if (!Directory.Exists("Scripts")) return;

			var files = Directory
				.GetFiles("Scripts", "*.sql", SearchOption.AllDirectories)
				.OrderBy(t => Path.GetFileName(t))
				.ToArray();
			if (files.Length <= 0) return;

			foreach(var file in files)
			{
				try
				{
					_logger.LogInformation($"Executing Script: {file}");
					var content = await File.ReadAllTextAsync(file);
					await con.ExecuteAsync(content);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error occurred while executing script: {file}");
				}
			}
		}
	}
}
