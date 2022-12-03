using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace CardboardBox.Anime.Database.Generation
{
	public class NpgsqlService : SqlService
	{
		private readonly IConfiguration _config;

		public override int Timeout => 0;

		public NpgsqlService(IConfiguration config)
		{
			_config = config;
		}

		public override IDbConnection CreateConnection()
		{
			var conString = _config["Postgres:ConnectionString"];
			var con = new NpgsqlConnection(conString);
			Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
			con.Open();
			con.ReloadTypes();

			con.TypeMapper.MapComposite<DbImage>("image");
			con.TypeMapper.MapComposite<DbExtension>("ext");
			con.TypeMapper.MapComposite<DbMangaAttribute>("manga_attribute");

			return con;
		}
	}
}
