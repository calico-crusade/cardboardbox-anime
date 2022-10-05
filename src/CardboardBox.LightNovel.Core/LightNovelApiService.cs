namespace CardboardBox.LightNovel.Core
{
	using Sources;

	public interface ILightNovelApiService
	{
		Task<Chapter[]?> FromJson(string path);
		ISourceService[] Sources();
	}

	public class LightNovelApiService : ILightNovelApiService
	{
		private readonly ISource1Service _src1;
		private readonly ISource2Service _src2;

		public LightNovelApiService(
			ISource1Service src1,
			ISource2Service src2)
		{
			_src1 = src1;
			_src2 = src2;
		}

		public async Task<Chapter[]?> FromJson(string path)
		{
			using var io = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<Chapter[]>(io).AsTask();
		}

		public ISourceService[] Sources() => new[] { (ISourceService)_src1, _src2 };
	}
}
